console.log("Script starting: category-attachments-references.js (v2.14)");

(function () {
    const urlParams = new URLSearchParams(window.location.search);
    const initialIsCreateMode = !urlParams.has('id');
    console.log("Initial isCreateMode:", initialIsCreateMode);

    window.attachments = [];
    window.references = [];
    window.deletedAttachmentIds = [];
    window.deletedReferenceIds = [];
    window.pendingFiles = [];
    window.isSubmitting = false;
    let isDirty = false; // Track unsaved changes
    window.isNavigatingSelf = false;

    // Debounce function to prevent rapid submissions
    function debounce(func, wait) {
        let timeout;
        return function executedFunction(...args) {
            const later = () => {
                clearTimeout(timeout);
                func(...args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
    }

    // Mark the form as dirty (has unsaved changes)
    function markAsDirty() {
        isDirty = true;
        console.log('Form marked as dirty');
    }

    // Clear the dirty flag after a successful save
    function clearDirtyFlag() {
        isDirty = false;
        window.deletedAttachmentIds = [];
        window.deletedReferenceIds = [];
        console.log('Form marked as clean (no unsaved changes)');
    }

    function saveStateToStorage() {
        try {
            const categoryId = document.querySelector('input[name="Id"]')?.value || 'create';
            sessionStorage.setItem(`categoryAttachments_${categoryId}`, JSON.stringify(window.attachments));
            sessionStorage.setItem(`categoryReferences_${categoryId}`, JSON.stringify(window.references));
            sessionStorage.setItem(`categoryPendingFiles_${categoryId}`, JSON.stringify(window.pendingFiles.map(pf => ({
                name: pf.name,
                type: pf.type,
                size: pf.size
                // Note: We can't store the actual File object in sessionStorage, only metadata
            }))));

            // Save TinyMCE editor content
            if (typeof tinymce !== "undefined" && tinymce.get("editor")) {
                tinymce.triggerSave();
                const editorContent = tinymce.get("editor").getContent();
                sessionStorage.setItem(`categoryEditorContent_${categoryId}`, editorContent);
                console.log(`Saved TinyMCE content to sessionStorage: ${editorContent}`);
            } else {
                console.warn("TinyMCE not initialized, skipping editor content save");
            }

            console.log('Saved to sessionStorage:', { attachments: window.attachments, references: window.references, pendingFiles: window.pendingFiles });
        } catch (e) {
            console.error('Error saving to sessionStorage:', e);
        }
    }

    window.clearSessionStorage = function () {
        const categoryId = document.querySelector('input[name="Id"]')?.value || 'create';
        sessionStorage.removeItem(`categoryAttachments_${categoryId}`);
        sessionStorage.removeItem(`categoryReferences_${categoryId}`);
        sessionStorage.removeItem(`categoryPendingFiles_${categoryId}`);
        sessionStorage.removeItem(`categoryEditorContent_${categoryId}`);
        console.log('Cleared sessionStorage for category:', categoryId);
    };

    function updateAttachmentData() {
        const attachmentDataInput = document.getElementById("attachmentData");
        if (attachmentDataInput) {
            const updatedAttachments = window.attachments.map(a => ({
                id: a.id || 0,
                fileName: a.fileName,
                caption: a.caption || '',
                isInternal: a.isInternal || false,
                isDeleted: a.isDeleted === true,
                isMarkedWithX: a.isMarkedWithX === true,
                fromParent: a.fromParent || false,
                parentAttachmentId: a.parentAttachmentId || 0
            })).filter(a => !a.isDeleted && !a.isMarkedWithX);
            attachmentDataInput.value = JSON.stringify(updatedAttachments);
            console.log("Updated attachmentData:", attachmentDataInput.value);
        }
        saveStateToStorage();
    }

    function updateReferenceData() {
        const referenceDataInput = document.getElementById("referenceData");
        if (referenceDataInput) {
            const updatedReferences = window.references.map(r => ({
                id: r.id || 0,
                url: r.url,
                description: r.description || '',
                isInternal: r.isInternal || false,
                openOption: r.openOption || '_self',
                isDeleted: r.isDeleted || false,
                isMarkedWithX: r.isMarkedWithX || false,
                fromParent: r.fromParent || false,
                parentReferenceId: r.parentReferenceId || 0
            })).filter(r => !r.isDeleted && !r.isMarkedWithX);
            referenceDataInput.value = JSON.stringify(updatedReferences);
            console.log("Updated referenceData:", referenceDataInput.value);
        }
        saveStateToStorage();
    }

    window.updateAttachmentData = updateAttachmentData;
    window.updateReferenceData = updateReferenceData;

    function restoreState() {
        const categoryId = document.querySelector('input[name="Id"]')?.value || 'create';
        const isEditMode = !!categoryId && categoryId !== 'create';
        console.log('restoreState: isEditMode=', isEditMode, 'categoryId=', categoryId);

        let sessionAttachments, sessionReferences, pendingFilesMeta;
        try {
            const savedAttachments = sessionStorage.getItem(`categoryAttachments_${categoryId}`);
            const savedReferences = sessionStorage.getItem(`categoryReferences_${categoryId}`);
            const savedPendingFiles = sessionStorage.getItem(`categoryPendingFiles_${categoryId}`);

            sessionAttachments = savedAttachments ? JSON.parse(savedAttachments) : [];
            sessionReferences = savedReferences ? JSON.parse(savedReferences) : [];
            pendingFilesMeta = savedPendingFiles ? JSON.parse(savedPendingFiles) : [];

            sessionAttachments = sessionAttachments.filter(a => a && a.fileName && typeof a.isDeleted === 'boolean');
            sessionReferences = sessionReferences.filter(r => r && r.url && typeof r.isDeleted === 'boolean');
            console.log('Validated sessionStorage data:', { sessionAttachments, sessionReferences, pendingFilesMeta });
        } catch (e) {
            console.error('Error parsing sessionStorage data:', e);
            sessionAttachments = [];
            sessionReferences = [];
            pendingFilesMeta = [];
        }

        // Always initialize from DOM first to ensure all parent attachments/references are included
        console.log('Initializing from DOM before merging with sessionStorage');
        initializeFromDOM();

        // If there is sessionStorage data, merge it with the DOM-initialized data
        if (sessionAttachments.length > 0 || sessionReferences.length > 0 || pendingFilesMeta.length > 0) {
            console.log('Merging sessionStorage data with DOM-initialized data');

            // Merge attachments: Update existing or add new ones from sessionStorage
            sessionAttachments.forEach(sessionAtt => {
                const existingIndex = window.attachments.findIndex(a =>
                    (sessionAtt.fromParent && sessionAtt.parentAttachmentId && a.parentAttachmentId === sessionAtt.parentAttachmentId) ||
                    (!sessionAtt.fromParent && a.fileName === sessionAtt.fileName)
                );
                if (existingIndex !== -1) {
                    // Update existing attachment with sessionStorage data
                    window.attachments[existingIndex] = { ...window.attachments[existingIndex], ...sessionAtt };
                    console.log(`Merged sessionStorage attachment: fileName=${sessionAtt.fileName}, fromParent=${sessionAtt.fromParent}, parentAttachmentId=${sessionAtt.parentAttachmentId}`);
                } else {
                    // Add new attachment (e.g., user-added attachment not in DOM)
                    window.attachments.push(sessionAtt);
                    console.log(`Added sessionStorage attachment: fileName=${sessionAtt.fileName}, fromParent=${sessionAtt.fromParent}, parentAttachmentId=${sessionAtt.parentAttachmentId}`);
                }
            });

            // Merge references: Update existing or add new ones from sessionStorage
            sessionReferences.forEach(sessionRef => {
                const existingIndex = window.references.findIndex(r =>
                    (sessionRef.fromParent && sessionRef.parentReferenceId && r.parentReferenceId === sessionRef.parentReferenceId) ||
                    (!sessionRef.fromParent && r.url === sessionRef.url)
                );
                if (existingIndex !== -1) {
                    // Update existing reference with sessionStorage data
                    window.references[existingIndex] = { ...window.references[existingIndex], ...sessionRef };
                    console.log(`Merged sessionStorage reference: url=${sessionRef.url}, fromParent=${sessionRef.fromParent}, parentReferenceId=${sessionRef.parentReferenceId}`);
                } else {
                    // Add new reference (e.g., user-added reference not in DOM)
                    window.references.push(sessionRef);
                    console.log(`Added sessionStorage reference: url=${sessionRef.url}, fromParent=${sessionRef.fromParent}, parentReferenceId=${sessionRef.parentReferenceId}`);
                }
            });

            // Restore pendingFiles metadata (but File objects can't be restored)
            if (pendingFilesMeta.length > 0) {
                console.log('Pending files metadata restored from sessionStorage (File objects not restored):', pendingFilesMeta);
            }
        } else {
            console.log('No sessionStorage data to merge, using DOM-initialized data');
        }

        // Reindex to update the UI
        const attachmentList = document.getElementById("attachmentList");
        const referenceList = document.getElementById("referenceList");
        if (attachmentList) {
            attachmentList.innerHTML = "";
            console.log('Cleared attachmentList');
        }
        if (referenceList) {
            referenceList.innerHTML = "";
            console.log('Cleared referenceList');
        }

        window.reindexAttachments();
        window.reindexReferences();
        updateAttachmentData();
        updateReferenceData();
    }

    function initializeFromDOM() {
        window.attachments = [];
        window.references = [];
        console.log('Preserving existing window.pendingFiles during initializeFromDOM:', window.pendingFiles);

        // Initialize attachments from DOM
        document.querySelectorAll('#attachmentList li').forEach((li, index) => {
            const attachmentId = parseInt(li.dataset.attachmentId) || 0;
            const anchor = li.querySelector('a.attachment-link');
            const fileName = anchor ? anchor.textContent : li.querySelector('strong')?.textContent || 'Unnamed';
            const attachment = {
                id: initialIsCreateMode ? 0 : attachmentId,
                fileName: fileName,
                url: anchor ? anchor.getAttribute('href') : null,
                caption: li.querySelector('.caption-input')?.value || '',
                isInternal: li.querySelector('.internal-attachment')?.checked || false,
                isDeleted: false,
                isMarkedWithX: false,
                fromParent: attachmentId > 0,
                parentAttachmentId: attachmentId
            };
            if (attachment.fileName && !window.attachments.some(a => a.fileName === attachment.fileName && !a.isDeleted)) {
                window.attachments.push(attachment);
            }
            console.log(`Initialized attachment ${index} from DOM: fileName=${attachment.fileName}, url=${attachment.url}, fromParent=${attachment.fromParent}, id=${attachment.id}, parentAttachmentId=${attachment.parentAttachmentId}`);
            addAttachmentEventListeners(li, index);
        });

        // Initialize from server data if no DOM attachments
        if (window.attachments.length === 0) {
            const attachmentLinks = document.querySelector('#attachmentLinks')?.value;
            if (attachmentLinks) {
                try {
                    const attachmentsFromServer = JSON.parse(attachmentLinks);
                    attachmentsFromServer.forEach((attachment, index) => {
                        if (!attachment.isDeleted) {
                            const att = {
                                id: initialIsCreateMode ? 0 : (attachment.id || 0),
                                fileName: attachment.fileName,
                                url: attachment.filePath || null,
                                caption: attachment.caption || '',
                                isInternal: attachment.isInternal || false,
                                isDeleted: false,
                                isMarkedWithX: false,
                                fromParent: true,
                                parentAttachmentId: attachment.id || 0
                            };
                            if (att.fileName && !window.attachments.some(a => a.fileName === att.fileName && !a.isDeleted)) {
                                window.attachments.push(att);
                                console.log(`Initialized attachment ${index} from server data: fileName=${att.fileName}, url=${att.url}, fromParent=${att.fromParent}, id=${att.id}, parentAttachmentId=${att.parentAttachmentId}`);
                            }
                        }
                    });
                    window.reindexAttachments();
                } catch (e) {
                    console.error('Error parsing attachmentLinks:', e);
                }
            }
        }

        // Initialize references from DOM
        document.querySelectorAll('#referenceList li').forEach((li, index) => {
            const referenceId = parseInt(li.dataset.referenceId) || 0;
            const anchor = li.querySelector('a.reference-link');
            const openOption = anchor?.getAttribute('target') || '_self';
            const url = anchor?.getAttribute('href') || anchor?.textContent || '';
            const reference = {
                id: initialIsCreateMode ? 0 : referenceId,
                url: url,
                description: li.querySelector('.description-input')?.value || '',
                isInternal: li.querySelector('.internal-reference')?.checked || false,
                openOption: openOption,
                isDeleted: false,
                isMarkedWithX: false,
                fromParent: referenceId > 0,
                parentReferenceId: referenceId
            };
            if (reference.url && !window.references.some(r => r.url === reference.url && !r.isDeleted)) {
                window.references.push(reference);
            }
            console.log(`Initialized reference ${index}: url=${reference.url}, openOption=${reference.openOption}, fromParent=${reference.fromParent}, id=${reference.id}, parentReferenceId=${reference.parentReferenceId}`);
            addReferenceEventListeners(li, index);
        });

        console.log("Initialized from DOM:", { attachments: window.attachments, references: window.references });
        updateAttachmentData();
        updateReferenceData();
    }

    window.addEventListener('pageshow', function (event) {
        console.log('pageshow triggered, persisted:', event.persisted);
        restoreState();
    });

    // Targeted click handler for reference links
    const referenceList = document.getElementById('referenceList');
    if (referenceList) {
        console.log("Attaching click handler to #referenceList");
        referenceList.addEventListener('click', function (e) {
            const anchor = e.target.closest('a.reference-link');
            if (!anchor) {
                console.log("Click ignored: Not a reference link", e.target);
                return;
            }

            console.log("Reference link clicked:", {
                href: anchor.href,
                target: anchor.target,
                text: anchor.textContent,
                classList: anchor.className
            });

            e.preventDefault();
            e.stopPropagation();

            const li = anchor.closest('li');
            const index = parseInt(li.querySelector('.delete-reference')?.dataset.index);
            console.log("Reference index:", index);

            if (isNaN(index) || !window.references[index]) {
                console.error("Invalid reference index:", index, window.references);
                toastr.error('Unable to navigate: Reference not found.');
                return;
            }

            const reference = window.references[index];
            console.log("Reference details:", reference);

            // Validate URL
            let url = reference.url;
            try {
                new URL(url);
            } catch (err) {
                console.error("Invalid URL:", url, err);
                toastr.error('Invalid URL: ' + url);
                return;
            }

            // Debug navigation
            console.log(`Attempting navigation to: ${url} (openOption: ${reference.openOption})`);

            // Save state, including TinyMCE content
            saveStateToStorage();

            // Navigate
            try {
                if (reference.openOption === '_blank') {
                    console.log("Opening in new tab:", url);
                    window.open(url, '_blank');
                } else {
                    console.log("Navigating in current tab:", url);
                    window.isNavigatingSelf = true;
                    window.location.assign(url);
                }
            } catch (err) {
                console.error("Navigation failed:", err);
                if (typeof toastr !== 'undefined') {
                    toastr.error('Navigation failed: ' + err.message);
                } else {
                    alert('Navigation failed: ' + err.message);
                }
                window.isNavigatingSelf = false;
            }
        });
    } else {
        console.error("referenceList element not found!");
    }

    const uploadBtn = document.getElementById("uploadAttachmentBtn");
    if (uploadBtn) {
        uploadBtn.addEventListener("click", function (e) {
            e.preventDefault();
            console.log("uploadAttachmentBtn clicked");
            document.getElementById("attachmentInput").click();
        });
    } else {
        console.error("uploadAttachmentBtn not found!");
    }

    const attachmentInput = document.getElementById("attachmentInput");
    if (attachmentInput) {
        attachmentInput.addEventListener("change", function (event) {
            console.log("attachmentInput changed");
            const files = event.target.files;
            if (!files || files.length === 0) return;

            const allowedTypes = ['image/jpeg', 'image/png', 'application/pdf', 'text/plain'];

            for (let file of files) {
                if (!allowedTypes.includes(file.type)) {
                    toastr.error(`File ${file.name} has an unsupported type. Allowed types: JPEG, PNG, PDF, TXT.`);
                    continue;
                }
                // Check if the file already exists in window.attachments or window.pendingFiles
                if (!window.attachments.some(a => a.fileName === file.name && !a.isDeleted) &&
                    !window.pendingFiles.some(pf => pf.name === file.name)) {
                    // Create a temporary URL for the file to make it downloadable in the UI
                    const tempUrl = URL.createObjectURL(file);
                    const attachment = {
                        id: 0,
                        fileName: file.name,
                        url: tempUrl, // Temporary URL for UI display
                        caption: "",
                        isInternal: false,
                        isDeleted: false,
                        fromParent: false,
                        parentAttachmentId: 0,
                        tempUrl: true // Flag to indicate this is a temporary URL
                    };
                    window.attachments.push(attachment);
                    window.pendingFiles.push(file); // Store the File object directly
                    addAttachmentToList(attachment, window.attachments.length - 1);
                    console.log(`Added uploaded attachment: fileName=${file.name}, type=${file.type}, size=${file.size}, tempUrl=${tempUrl}`);
                } else {
                    console.log(`Attachment already exists: ${file.name}`);
                }
            }
            updateAttachmentData();
            markAsDirty();
        });
    } else {
        console.error("attachmentInput not found!");
    }

    const addRefBtn = document.getElementById("addReferenceBtn");
    if (addRefBtn) {
        addRefBtn.addEventListener("click", function (e) {
            e.preventDefault();
            console.log("addReferenceBtn clicked");
            let refUrl = prompt("Enter reference link:");
            if (!refUrl) return;

            if (!refUrl.startsWith('http://') && !refUrl.startsWith('https://')) {
                refUrl = 'https://' + refUrl;
            }

            try {
                new URL(refUrl);
            } catch (err) {
                console.error('Invalid URL entered:', refUrl, err);
                toastr.error('Please enter a valid URL (e.g., https://example.com).');
                return;
            }

            const openInNewWindow = confirm("Should this reference open in a new window?");
            const reference = {
                id: 0,
                url: refUrl,
                description: "",
                isInternal: false,
                openOption: openInNewWindow ? "_blank" : "_self",
                isDeleted: false,
                fromParent: false,
                parentReferenceId: 0
            };
            window.references.push(reference);
            addReferenceToList(reference, window.references.length - 1);
            updateReferenceData();
            console.log(`Added manual reference: url=${refUrl}, openOption=${reference.openOption}`);
            markAsDirty();
        });
    } else {
        console.error("addReferenceBtn not found!");
    }

    document.getElementById("attachmentList")?.addEventListener("change", function (e) {
        if (e.target.classList.contains("internal-attachment")) {
            const index = parseInt(e.target.dataset.index);
            window.attachments[index].isInternal = e.target.checked;
            console.log(`Updated attachment ${index}: isInternal=${e.target.checked}`);
            updateAttachmentData();
            markAsDirty();
        }
    });

    document.getElementById("referenceList")?.addEventListener("change", function (e) {
        if (e.target.classList.contains("internal-reference")) {
            const index = parseInt(e.target.dataset.index);
            window.references[index].isInternal = e.target.checked;
            console.log(`Updated reference ${index}: isInternal=${e.target.checked}`);
            updateReferenceData();
            markAsDirty();
        }
    });

    document.getElementById("attachmentList")?.addEventListener("click", function (e) {
        if (e.target.classList.contains("delete-attachment")) {
            e.preventDefault();
            const index = parseInt(e.target.dataset.index);
            console.log(`Marking attachment for deletion: index=${index}`);
            if (window.attachments[index]) {
                const attachment = window.attachments[index];
                const attachmentId = attachment.id;
                const parentAttachmentId = attachment.parentAttachmentId;
                const isFromParent = attachment.fromParent;

                // Revoke the temporary URL if it exists
                if (attachment.tempUrl && attachment.url) {
                    URL.revokeObjectURL(attachment.url);
                    console.log(`Revoked temporary URL for attachment: ${attachment.fileName}`);
                }

                if (isFromParent && parentAttachmentId > 0) {
                    console.log(`Parent-sourced attachment: marking locally with isMarkedWithX, parentAttachmentId=${parentAttachmentId}`);
                    window.attachments[index].isDeleted = true;
                    window.attachments[index].isMarkedWithX = true;
                    if (!window.deletedAttachmentIds.includes(parentAttachmentId)) {
                        window.deletedAttachmentIds.push(parentAttachmentId);
                    }
                    window.pendingFiles = window.pendingFiles.filter(pf => pf.name !== attachment.fileName);
                    window.reindexAttachments();
                    toastr.success("Parent attachment marked for deletion. Save the form to finalize.");
                } else if (attachmentId > 0) {
                    jQuery.post('/Admin/Category/RemoveAttachment', { id: attachmentId }, function (response) {
                        console.log("RemoveAttachment response:", response);
                        if (response && response.success) {
                            console.log("Successfully marked attachment for deletion:", attachmentId);
                            if (!window.deletedAttachmentIds.includes(attachmentId)) {
                                window.deletedAttachmentIds.push(attachmentId);
                            }
                            window.attachments[index].isDeleted = true;
                            window.attachments[index].isMarkedWithX = true;
                            window.pendingFiles = window.pendingFiles.filter(pf => pf.name !== attachment.fileName);
                            window.reindexAttachments();
                            toastr.success("Attachment marked for deletion. Save the form to finalize.");
                        } else {
                            console.error("Failed to mark attachment for deletion:", response ? response.message : "No response");
                            toastr.error(response ? response.message : "Failed to mark attachment for deletion.");
                        }
                    }).fail(function (xhr, status, error) {
                        console.error("Error in RemoveAttachment request:", status, error, xhr.responseText);
                        toastr.error("An error occurred while marking the attachment for deletion.");
                    });
                } else {
                    console.log("Removing unsaved attachment at index:", index);
                    window.attachments[index].isDeleted = true;
                    window.attachments[index].isMarkedWithX = true;
                    window.pendingFiles = window.pendingFiles.filter(pf => pf.name !== attachment.fileName);
                    window.reindexAttachments();
                    toastr.success("Attachment removed. Save the form to finalize.");
                }
                updateAttachmentData();
                markAsDirty();
            } else {
                console.warn(`Attachment at index ${index} not found`);
            }
        }
    });

    document.getElementById("referenceList")?.addEventListener("click", function (e) {
        if (e.target.classList.contains("delete-reference")) {
            e.preventDefault();
            const index = parseInt(e.target.dataset.index);
            console.log(`Marking reference for deletion: index=${index}`);
            if (window.references[index]) {
                const reference = window.references[index];
                const referenceId = reference.id;
                const parentReferenceId = reference.parentReferenceId;
                const isFromParent = reference.fromParent;

                if (isFromParent && parentReferenceId > 0) {
                    console.log(`Parent-sourced reference: marking locally with isMarkedWithX, parentReferenceId=${parentReferenceId}`);
                    window.references[index].isDeleted = true;
                    window.references[index].isMarkedWithX = true;
                    if (!window.deletedReferenceIds.includes(parentReferenceId)) {
                        window.deletedReferenceIds.push(parentReferenceId);
                    }
                    window.reindexReferences();
                    toastr.success("Parent reference marked for deletion. Save the form to finalize.");
                } else if (referenceId > 0) {
                    jQuery.post('/Admin/Category/RemoveReference', { id: referenceId }, function (response) {
                        console.log("RemoveReference response:", response);
                        if (response && response.success) {
                            console.log("Successfully marked reference for deletion:", referenceId);
                            if (!window.deletedReferenceIds.includes(referenceId)) {
                                window.deletedReferenceIds.push(referenceId);
                            }
                            window.references[index].isDeleted = true;
                            window.references[index].isMarkedWithX = true;
                            window.reindexReferences();
                            toastr.success("Reference marked for deletion. Save the form to finalize.");
                        } else {
                            console.error("Failed to mark reference for deletion:", response ? response.message : "No response");
                            toastr.error(response ? response.message : "Failed to mark reference for deletion.");
                        }
                    }).fail(function (xhr, status, error) {
                        console.error("Error in RemoveReference request:", status, error, xhr.responseText);
                        toastr.error("An error occurred while marking the reference for deletion.");
                    });
                } else {
                    console.log("Removing unsaved reference at index:", index);
                    window.references[index].isDeleted = true;
                    window.references[index].isMarkedWithX = true;
                    window.reindexReferences();
                    toastr.success("Reference removed. Save the form to finalize.");
                }
                updateReferenceData();
                markAsDirty();
            } else {
                console.warn(`Reference at index ${index} not found`);
            }
        }
    });

    function addAttachmentToList(attachment, index) {
        if (attachment.isDeleted || attachment.isMarkedWithX || !attachment.fileName) return;
        let attachmentList = document.getElementById("attachmentList");
        let li = document.createElement("li");
        li.id = `attachment-${index}`;
        li.className = "list-group-item d-flex justify-content-between align-items-center";
        li.dataset.attachmentId = attachment.parentAttachmentId || attachment.id || 0;
        const fileName = attachment.fileName || 'Unnamed_Attachment';

        // Determine if the file is an image based on its extension
        const extension = fileName.toLowerCase().split('.').pop();
        const isImage = ['jpg', 'jpeg', 'png', 'gif'].includes(extension);

        let fileNameHtml;
        if (attachment.url) {
            if (isImage) {
                // For images (saved or new), open in a new tab without download attribute
                fileNameHtml = `<a href='${attachment.url}' target="_blank" class="attachment-link">${fileName}</a>`;
            } else if (attachment.id > 0 || attachment.fromParent) {
                // For non-image saved attachments, open in a new tab
                fileNameHtml = `<a href='${attachment.url}' target="_blank" class="attachment-link">${fileName}</a>`;
            } else {
                // For non-image new attachments, retain the download attribute
                fileNameHtml = `<a href='${attachment.url}' download="${fileName}" class="attachment-link">${fileName}</a>`;
            }
        } else {
            fileNameHtml = `<strong>${fileName}</strong>`;
        }

        li.innerHTML = `
        <div>
            ${fileNameHtml}<br />
            <input type="text" class="form-control mt-1 caption-input" placeholder="Enter caption" value="${attachment.caption || ''}" data-index="${index}" />
        </div>
        <div>
            <input type="checkbox" class="form-check-input internal-attachment" data-index="${index}" ${attachment.isInternal ? "checked" : ""} />
            <span>Internal</span>
            <span class="text-danger delete-attachment ms-3" style="cursor:pointer;" data-index="${index}">❌</span>
        </div>
    `;
        addAttachmentEventListeners(li, index);
        attachmentList.appendChild(li);
        console.log(`Added attachment to list: index=${index}, fileName=${fileName}, url=${attachment.url}, fromParent=${attachment.fromParent}, id=${attachment.id}, parentAttachmentId=${attachment.parentAttachmentId}, isImage=${isImage}`);
    }

    function addReferenceToList(reference, index) {
        if (reference.isDeleted || reference.isMarkedWithX || !reference.url) return;
        let referenceList = document.getElementById("referenceList");
        let li = document.createElement("li");
        li.id = `reference-${index}`;
        li.className = "list-group-item d-flex justify-content-between align-items-center";
        li.dataset.referenceId = reference.parentReferenceId || reference.id || 0;
        li.innerHTML = `
        <div>
            <a href="${reference.url}" target="${reference.openOption}" class="reference-link">${reference.url}</a><br />
            <input type="text" class="form-control mt-1 description-input" placeholder="Enter description" value="${reference.description || ''}" data-index="${index}" />
        </div>
        <div>
            <input type="checkbox" class="form-check-input internal-reference" data-index="${index}" ${reference.isInternal ? "checked" : ""} />
            <span>Internal</span>
            <span class="text-danger delete-reference ms-3" style="cursor:pointer;" data-index="${index}">❌</span>
        </div>
    `;
        addReferenceEventListeners(li, index);
        referenceList.appendChild(li);
        console.log(`Added reference to list: index=${index}, url=${reference.url}, openOption=${reference.openOption}, fromParent=${reference.fromParent}, id=${reference.id}, parentReferenceId=${reference.parentReferenceId}`);
    }

    function addAttachmentEventListeners(li, index) {
        const captionInput = li.querySelector(".caption-input");
        if (captionInput) {
            captionInput.addEventListener("input", function () {
                window.attachments[index].caption = this.value;
                console.log(`Updated attachment caption: index=${index}, caption=${this.value}`);
                updateAttachmentData();
                markAsDirty();
            });
        }
    }

    function addReferenceEventListeners(li, index) {
        const descriptionInput = li.querySelector(".description-input");
        if (descriptionInput) {
            descriptionInput.addEventListener("input", function () {
                window.references[index].description = this.value;
                console.log(`Updated reference description: index=${index}, description=${this.value}`);
                updateReferenceData();
                markAsDirty();
            });
        }
    }

    window.reindexAttachments = function () {
        let attachmentList = document.getElementById("attachmentList");
        attachmentList.innerHTML = "";
        window.attachments.forEach((attachment, index) => {
            if (!attachment.isDeleted && attachment.fileName) {
                addAttachmentToList(attachment, index);
            }
        });
        console.log("Reindexed attachments:", window.attachments);
        updateAttachmentData();
    };

    window.reindexReferences = function () {
        let referenceList = document.getElementById("referenceList");
        referenceList.innerHTML = "";
        window.references.forEach((reference, index) => {
            if (!reference.isDeleted && reference.url) {
                addReferenceToList(reference, index);
            }
        });
        console.log("Reindexed references:", window.references);
        updateReferenceData();
    };

    window.updateAttachmentsAndReferences = function (attachmentsFromCategory, referencesFromCategory) {
        console.log("updateAttachmentsAndReferences called with:", {
            attachments: attachmentsFromCategory,
            references: referencesFromCategory
        });

        if (!Array.isArray(attachmentsFromCategory)) {
            console.warn("attachmentsFromCategory is not an array:", attachmentsFromCategory);
            attachmentsFromCategory = [];
        }
        if (!Array.isArray(referencesFromCategory)) {
            console.warn("referencesFromCategory is not an array:", referencesFromCategory);
            referencesFromCategory = [];
        }

        // Preserve only user-added attachments/references (not from parent)
        const userAddedAttachments = window.attachments.filter(a => !a.fromParent && !a.isDeleted && !a.isMarkedWithX);
        const userAddedReferences = window.references.filter(r => !r.fromParent && !r.isDeleted && !r.isMarkedWithX);

        // Clear existing attachments/references and initialize with user-added ones
        window.attachments = userAddedAttachments;
        window.references = userAddedReferences;
        window.pendingFiles = window.pendingFiles.filter(f => window.attachments.some(a => a.fileName === f.name && !a.isDeleted && !a.isMarkedWithX));
        console.log('Preserved pendingFiles after updateAttachmentsAndReferences:', window.pendingFiles);

        // Add new parent category attachments
        attachmentsFromCategory.forEach(att => {
            if (!att.fileName) {
                console.warn("Skipping invalid attachment (missing fileName):", att);
                return;
            }
            if (!window.attachments.some(a => a.fileName === att.fileName && a.parentAttachmentId === att.id && !a.isDeleted && !a.isMarkedWithX)) {
                window.attachments.push({
                    id: initialIsCreateMode ? 0 : (att.id || 0),
                    fileName: att.fileName,
                    url: att.url || null,
                    caption: att.caption || '',
                    isInternal: att.isInternal || false,
                    isDeleted: false,
                    isMarkedWithX: false,
                    fromParent: true,
                    parentAttachmentId: att.id || 0
                });
            }
        });

        // Add new parent category references
        referencesFromCategory.forEach(ref => {
            if (!ref.url) {
                console.warn("Skipping invalid reference (missing url):", ref);
                return;
            }
            if (!window.references.some(r => r.url === ref.url && r.parentReferenceId === ref.id && !r.isDeleted && !r.isMarkedWithX)) {
                window.references.push({
                    id: initialIsCreateMode ? 0 : (ref.id || 0),
                    url: ref.url,
                    description: ref.description || '',
                    isInternal: ref.isInternal || false,
                    openOption: ref.openOption || '_self',
                    isDeleted: false,
                    isMarkedWithX: false,
                    fromParent: true,
                    parentReferenceId: ref.id || 0
                });
            }
        });

        console.log("After updating:", {
            attachments: window.attachments,
            references: window.references,
            pendingFiles: window.pendingFiles
        });

        window.reindexAttachments();
        window.reindexReferences();
    };

    const handleFormSubmit = debounce(function (e) {
        e.preventDefault();
        e.stopImmediatePropagation();
        console.log("Form submission triggered");

        if (window.isSubmitting) {
            console.log('Form submission prevented: Already submitting');
            toastr.warning('Submission in progress. Please wait.');
            return;
        }
        window.isSubmitting = true;

        const saveButton = document.getElementById("saveButton");
        if (saveButton) {
            saveButton.disabled = true;
            saveButton.textContent = initialIsCreateMode ? 'Saving' : 'Updating';
        }

        const parentCategoryInput = document.getElementById("parentCategoryDropdown");
        const parentCategoryId = parentCategoryInput?.value;
        console.log("Locked ParentCategoryId:", parentCategoryId);
        if (parentCategoryInput) {
            parentCategoryInput.disabled = true;
        }

        if (parentCategoryId) {
            parentCategoryInput.setAttribute('value', parentCategoryId);
            console.log("Set parentCategoryDropdown value attribute to:", parentCategoryId);
        }

        if (typeof tinymce !== "undefined" && tinymce.get("editor")) {
            tinymce.triggerSave();
            const editorContent = tinymce.get("editor").getContent();
            document.getElementById("HtmlContent").value = editorContent;
            console.log("TinyMCE content saved:", editorContent);
        } else {
            console.warn("TinyMCE not initialized, proceeding without editor content");
        }

        const name = document.getElementById("Name")?.value;
        if (!name) {
            toastr.error('Please fill all required fields.');
            window.isSubmitting = false;
            if (saveButton) {
                saveButton.disabled = false;
                saveButton.textContent = initialIsCreateMode ? 'Save' : 'Update';
            }
            if (parentCategoryInput) {
                parentCategoryInput.disabled = false;
            }
            return;
        }

        updateAttachmentData();
        updateReferenceData();

        // Log the final AttachmentData and ReferenceData before submission
        const attachmentDataInput = document.getElementById("attachmentData");
        const referenceDataInput = document.getElementById("referenceData");
        console.log("Final AttachmentData before submission:", attachmentDataInput?.value);
        console.log("Final ReferenceData before submission:", referenceDataInput?.value);

        window.deletedAttachmentIds.forEach(id => {
            const input = document.createElement("input");
            input.type = "hidden";
            input.name = "deletedAttachmentIds";
            input.value = id;
            this.appendChild(input);
            console.log("Added deletedAttachmentIds input:", id);
        });

        window.deletedReferenceIds.forEach(id => {
            const input = document.createElement("input");
            input.type = "hidden";
            input.name = "deletedReferenceIds";
            input.value = id;
            this.appendChild(input);
            console.log("Added deletedReferenceIds input:", id);
        });

        console.log("Deleted Attachment IDs:", window.deletedAttachmentIds);
        console.log("Deleted Reference IDs:", window.deletedReferenceIds);

        const form = this;
        const formData = new FormData(form);

        // Ensure all pending files are appended to FormData with unique keys
        console.log("Pending files before submission:", window.pendingFiles);
        window.pendingFiles.forEach((pf, index) => {
            if (window.attachments.some(a => a.fileName === pf.name && !a.isDeleted)) {
                // Use a unique key for each file to ensure the server receives all files
                formData.append(`files[${index}]`, pf, pf.name);
                console.log(`Appended file to FormData: key=files[${index}], name=${pf.name}, type=${pf.type}, size=${pf.size}`);
            } else {
                console.warn(`Skipping file append: ${pf.name} (attachment deleted)`);
            }
        });

        formData.append("submitAction", "Save");

        if (parentCategoryId) {
            formData.append("ParentCategoryId", parentCategoryId);
            console.log("Manually appended ParentCategoryId to FormData:", parentCategoryId);
        }

        console.log("FormData entries before submission:");
        for (let [key, value] of formData.entries()) {
            console.log(`${key}: ${value instanceof File ? `${value.name} (File, ${value.type}, ${value.size} bytes)` : value}`);
        }

        // Determine if this is Create or Edit mode based on the presence of Id
        const categoryId = document.querySelector('input[name="Id"]')?.value;
        const isCreateMode = !categoryId || categoryId === 'create';
        console.log("isCreateMode at submission:", isCreateMode);

        jQuery.ajax({
            url: form.action,
            type: form.method,
            data: formData,
            processData: false,
            contentType: false,
            beforeSend: function () {
                console.log("Initiating AJAX request to:", form.action);
            },
            success: function (response) {
                console.log("Form submission response:", response);
                if (response.success) {
                    // Update attachments with server-provided URLs if available
                    if (response.attachments && Array.isArray(response.attachments)) {
                        response.attachments.forEach(serverAtt => {
                            const matchingAtt = window.attachments.find(a => a.fileName === serverAtt.fileName && !a.isDeleted);
                            if (matchingAtt) {
                                // Revoke temporary URL if it exists
                                if (matchingAtt.tempUrl && matchingAtt.url) {
                                    URL.revokeObjectURL(matchingAtt.url);
                                    console.log(`Revoked temporary URL for attachment: ${matchingAtt.fileName}`);
                                }
                                matchingAtt.id = serverAtt.id || matchingAtt.id;
                                matchingAtt.url = serverAtt.filePath || serverAtt.url || matchingAtt.url;
                                matchingAtt.tempUrl = false; // Clear tempUrl flag
                                console.log(`Updated attachment URL: fileName=${matchingAtt.fileName}, url=${matchingAtt.url}`);
                            }
                        });
                        window.reindexAttachments();
                    }

                    window.clearSessionStorage();
                    clearDirtyFlag(); // Clear the dirty flag on successful submission

                    // Clear the file input and pending files after successful submission
                    const attachmentInput = document.getElementById("attachmentInput");
                    if (attachmentInput) {
                        attachmentInput.value = ''; // Clear the input
                        console.log("Cleared attachmentInput files");
                    }
                    window.pendingFiles = []; // Clear pendingFiles after successful submission
                    console.log("Cleared window.pendingFiles after successful submission");

                    toastr.success(isCreateMode ? "Category created successfully!" : "Category updated successfully!");
                    console.log("Redirecting to:", response.redirectUrl);
                    window.location.href = response.redirectUrl;
                } else {
                    toastr.error(response.message || "Failed to submit form.");
                    if (response.errors) {
                        toastr.error("Validation errors: " + response.errors.join(", "));
                    }
                    window.isSubmitting = false;
                    if (saveButton) {
                        saveButton.disabled = false;
                        saveButton.textContent = isCreateMode ? 'Save' : 'Update';
                    }
                    if (parentCategoryInput) {
                        parentCategoryInput.disabled = false;
                    }
                }
            },
            error: function (xhr, status, error) {
                console.error("Error submitting form:", error, xhr.responseText);
                const errorMessage = xhr.responseJSON?.message || xhr.responseText || "An error occurred while submitting the form.";
                toastr.error(`Error: ${errorMessage}`);
                window.isSubmitting = false;
                if (saveButton) {
                    saveButton.disabled = false;
                    saveButton.textContent = isCreateMode ? 'Save' : 'Update';
                }
                if (parentCategoryInput) {
                    parentCategoryInput.disabled = false;
                }
            },
            complete: function () {
                // Do not clear window.pendingFiles here to preserve files in case of failure
                const attachmentInput = document.getElementById("attachmentInput");
                if (attachmentInput) {
                    attachmentInput.value = ''; // Clear the input
                    console.log("Cleared attachmentInput files in complete callback");
                }
                console.log("Preserving window.pendingFiles after submission attempt:", window.pendingFiles);
            }
        });
    }.bind(document.getElementById("categoryForm")), 500);

    const form = document.getElementById("categoryForm");
    if (form) {
        form.addEventListener("submit", handleFormSubmit);
        // Track changes in form inputs
        form.querySelectorAll('input, textarea, select').forEach(input => {
            if (input.id !== 'attachmentInput') { // Exclude attachmentInput since it's handled separately
                input.addEventListener('change', function () {
                    console.log(`${input.id || input.name} changed, marking as dirty`);
                    markAsDirty();
                });
            }
        });
    }

    const saveButton = document.getElementById("saveButton");
    if (saveButton) {
        saveButton.addEventListener("click", function (e) {
            e.preventDefault();
            console.log("Save button clicked, triggering form submission");
            if (form) {
                const submitEvent = new Event('submit', { cancelable: true });
                form.dispatchEvent(submitEvent);
            }
        });
    }

    // Track TinyMCE changes
    if (typeof tinymce !== "undefined") {
        tinymce.on('AddEditor', function (e) {
            const editor = e.editor;
            if (editor.id === 'editor') {
                editor.on('change keyup', function () {
                    console.log('TinyMCE content changed, marking as dirty');
                    markAsDirty();
                });
            }
        });
    }

    window.addEventListener('beforeunload', function (e) {
        if (isDirty && !window.isNavigatingSelf) {
            e.preventDefault();
            e.returnValue = "You have unsaved changes. Are you sure you want to leave?";
        }
    });

    window.forceRestoreState = restoreState;

    // Debug function for references
    window.debugReferences = function () {
        console.log("Debug: window.references:", window.references);
        console.log("Debug: #referenceList HTML:", document.getElementById("referenceList")?.innerHTML);
    };

    restoreState();
})();