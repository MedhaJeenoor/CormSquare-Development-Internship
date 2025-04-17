﻿console.log("Script starting: solution-attachments-references.js (v3.2)");

(function () {
    // Determine if in create mode (no solutionId in URL)
    const urlParams = new URLSearchParams(window.location.search);
    const isCreateMode = !urlParams.has('solutionId');
    console.log("isCreateMode:", isCreateMode);

    // Initialize global arrays
    window.attachments = [];
    window.references = [];

    // Function to save state to sessionStorage
    function saveStateToSessionStorage() {
        sessionStorage.setItem('solutionAttachments', JSON.stringify(window.attachments));
        sessionStorage.setItem('solutionReferences', JSON.stringify(window.references));
        console.log('Saved to sessionStorage:', { attachments: window.attachments, references: window.references });
    }

    // Function to update hidden inputs
    function updateAttachmentData() {
        const attachmentDataInput = document.getElementById("attachmentData");
        if (attachmentDataInput) {
            attachmentDataInput.value = JSON.stringify(window.attachments.map(a => ({
                id: a.id,
                fileName: a.fileName,
                caption: a.caption,
                isInternal: a.isInternal,
                isDeleted: a.isDeleted || false
            })));
            console.log("Updated AttachmentData:", attachmentDataInput.value);
        }
        saveStateToSessionStorage();
    }

    function updateReferenceData() {
        const referenceDataInput = document.getElementById("referenceData");
        if (referenceDataInput) {
            referenceDataInput.value = JSON.stringify(window.references.map(r => ({
                id: r.id || 0,
                url: r.url,
                description: r.description || '',
                isInternal: r.isInternal || false,
                openOption: r.openOption || '_self',
                isDeleted: r.isDeleted || false
            })));
            console.log("Updated ReferenceData:", referenceDataInput.value);
        }
        saveStateToSessionStorage();
    }

    // Expose update functions globally
    window.updateAttachmentData = updateAttachmentData;
    window.updateReferenceData = updateReferenceData;

    // Function to restore state and update UI
    function restoreState() {
        const savedAttachments = sessionStorage.getItem('solutionAttachments');
        const savedReferences = sessionStorage.getItem('solutionReferences');

        if (savedAttachments && savedReferences) {
            window.attachments = JSON.parse(savedAttachments);
            window.references = JSON.parse(savedReferences);
            console.log('Restored from sessionStorage:', { attachments: window.attachments, references: window.references });

            // Clear and re-render lists
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

            // Log UI state
            const attachmentCount = document.querySelectorAll('#attachmentList li').length;
            const referenceCount = document.querySelectorAll('#referenceList li').length;
            console.log(`UI sync check: ${attachmentCount} attachments, ${referenceCount} references in UI`);
        } else {
            // Initialize from DOM only if no sessionStorage data
            initializeFromDOM();
        }
    }

    // Force state restoration
    function forceRestoreState() {
        console.log('Forcing state restoration');
        restoreState();
    }

    // Initialize from DOM elements
    function initializeFromDOM() {
        window.attachments = [];
        window.references = [];

        document.querySelectorAll('#attachmentList li').forEach((li, index) => {
            const attachmentId = parseInt(li.dataset.attachmentId) || 0;
            const anchor = li.querySelector('a');
            const attachment = {
                id: attachmentId,
                fileName: anchor ? anchor.textContent : li.querySelector('strong').textContent,
                url: anchor ? anchor.getAttribute('href') : null,
                caption: li.querySelector('.caption-input').value,
                isInternal: li.querySelector('.internal-attachment').checked,
                isDeleted: false
            };
            if (!window.attachments.some(a => a.id === attachment.id && a.fileName === attachment.fileName)) {
                window.attachments.push(attachment);
            }
            console.log(`Initialized attachment ${index}: fileName=${attachment.fileName}, url=${attachment.url}`);
            addAttachmentEventListeners(li, index);
        });

        document.querySelectorAll('#referenceList li').forEach((li, index) => {
            const referenceId = parseInt(li.dataset.referenceId) || 0;
            const anchor = li.querySelector('a');
            const openOption = anchor.getAttribute('target') || '_self';
            const reference = {
                id: referenceId,
                url: anchor.textContent,
                description: li.querySelector('.description-input').value,
                isInternal: li.querySelector('.internal-reference').checked,
                openOption: openOption,
                isDeleted: false
            };
            if (!window.references.some(r => r.id === reference.id && r.url === reference.url)) {
                window.references.push(reference);
            }
            console.log(`Initialized reference ${index}: url=${reference.url}, openOption=${openOption}`);
            addReferenceEventListeners(li, index);
        });

        console.log("Initialized from DOM:", { attachments: window.attachments, references: window.references });
        updateAttachmentData();
        updateReferenceData();
    }

    // Handle page show and load events
    window.addEventListener('pageshow', function (event) {
        console.log('pageshow triggered, persisted:', event.persisted);
        forceRestoreState();
    });

    window.addEventListener('DOMContentLoaded', function () {
        console.log('DOMContentLoaded triggered');
        forceRestoreState();
    });

    // Clear sessionStorage on unload
    window.onunload = function () {
        console.log('onunload triggered');
        sessionStorage.removeItem('solutionAttachments');
        sessionStorage.removeItem('solutionReferences');
    };

    // Upload attachment button
    const uploadBtn = document.getElementById("uploadAttachmentBtn");
    if (uploadBtn) {
        uploadBtn.addEventListener("click", function (e) {
            e.preventDefault();
            document.getElementById("attachmentInput").click();
        });
    }

    // Handle file uploads
    const attachmentInput = document.getElementById("attachmentInput");
    if (attachmentInput) {
        attachmentInput.addEventListener("change", function (event) {
            const files = event.target.files;
            if (!files || files.length === 0) return;

            for (let file of files) {
                if (!window.attachments.some(a => a.fileName === file.name && !a.isDeleted)) {
                    const attachment = {
                        id: 0,
                        fileName: file.name,
                        url: null,
                        caption: "",
                        isInternal: false,
                        isDeleted: false
                    };
                    window.attachments.push(attachment);
                    console.log(`Added uploaded attachment: fileName=${file.name}`);
                    addAttachmentToList(attachment, window.attachments.length - 1);
                }
            }
            updateAttachmentData();
        });
    }

    // Add reference button
    const addRefBtn = document.getElementById("addReferenceBtn");
    if (addRefBtn) {
        addRefBtn.addEventListener("click", function (e) {
            e.preventDefault();
            const refUrl = prompt("Enter reference link:");
            if (!refUrl) return;
            const openInNewWindow = confirm("Should this reference open in a new window?");
            const openOption = openInNewWindow ? "_blank" : "_self";
            const reference = {
                id: 0,
                url: refUrl,
                description: "",
                isInternal: false,
                openOption: openOption,
                isDeleted: false
            };
            window.references.push(reference);
            console.log(`Added manual reference: url=${refUrl}, openOption=${openOption}`);
            addReferenceToList(reference, window.references.length - 1);
            updateReferenceData();
        });
    }

    // Event delegation for attachments
    document.getElementById("attachmentList").addEventListener("change", function (e) {
        if (e.target.classList.contains("internal-attachment")) {
            const index = parseInt(e.target.dataset.index);
            window.attachments[index].isInternal = e.target.checked;
            console.log(`Updated attachment ${index}: isInternal=${e.target.checked}`);
            updateAttachmentData();
        }
    });

    // Event delegation for references
    document.getElementById("referenceList").addEventListener("change", function (e) {
        if (e.target.classList.contains("internal-reference")) {
            const index = parseInt(e.target.dataset.index);
            window.references[index].isInternal = e.target.checked;
            console.log(`Updated reference ${index}: isInternal=${e.target.checked}`);
            updateReferenceData();
        }
    });

    function addAttachmentToList(attachment, index) {
        if (attachment.isDeleted) return;
        let attachmentList = document.getElementById("attachmentList");
        let li = document.createElement("li");
        li.id = `attachment-${index}`;
        li.className = "list-group-item d-flex justify-content-between align-items-center";
        li.dataset.attachmentId = attachment.id || 0;
        const fileName = attachment.fileName || 'Unnamed_Attachment';
        const fileNameHtml = attachment.url
            ? `<a href="${attachment.url}" download="${fileName}" onclick="console.log('Downloading: ${attachment.url}')">${fileName}</a>`
            : `<strong>${fileName}</strong>`;
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
        console.log(`Added attachment to list: index=${index}, fileName=${fileName}, url=${attachment.url}`);
        addAttachmentEventListeners(li, index);
        attachmentList.appendChild(li);
    }

    function addReferenceToList(reference, index) {
        if (reference.isDeleted) return;
        let referenceList = document.getElementById("referenceList");
        let li = document.createElement("li");
        li.id = `reference-${index}`;
        li.className = "list-group-item d-flex justify-content-between align-items-center";
        li.dataset.referenceId = reference.id || 0;
        li.innerHTML = `
            <div>
                <a href="${reference.url}" target="${reference.openOption}">${reference.url}</a><br />
                <input type="text" class="form-control mt-1 description-input" placeholder="Enter description" value="${reference.description || ''}" data-index="${index}" />
            </div>
            <div>
                <input type="checkbox" class="form-check-input internal-reference" data-index="${index}" ${reference.isInternal ? "checked" : ""} />
                <span>Internal</span>
                <span class="text-danger delete-reference ms-3" style="cursor:pointer;" data-index="${index}">❌</span>
            </div>
        `;
        console.log(`Added reference to list: index=${index}, url=${reference.url}, openOption=${reference.openOption}`);
        addReferenceEventListeners(li, index);
        referenceList.appendChild(li);
    }

    function addAttachmentEventListeners(li, index) {
        li.querySelector(".caption-input").addEventListener("input", function () {
            window.attachments[index].caption = this.value;
            console.log(`Updated attachment caption: index=${index}, caption=${this.value}`);
            updateAttachmentData();
        });
    }

    function addReferenceEventListeners(li, index) {
        li.querySelector(".description-input").addEventListener("input", function () {
            window.references[index].description = this.value;
            console.log(`Updated reference description: index=${index}, description=${this.value}`);
            updateReferenceData();
        });
    }

    window.reindexAttachments = function () {
        let attachmentList = document.getElementById("attachmentList");
        attachmentList.innerHTML = "";
        window.attachments.forEach((attachment, index) => {
            if (!attachment.isDeleted) {
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
            if (!reference.isDeleted) {
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

        if (!Array.isArray(referencesFromCategory)) {
            console.warn("referencesFromCategory is not an array:", referencesFromCategory);
            referencesFromCategory = [];
        }

        attachmentsFromCategory.forEach(att => {
            if (!window.attachments.some(a => a.fileName === att.fileName && a.id === att.id && !a.isDeleted)) {
                window.attachments.push({
                    id: att.id || 0,
                    fileName: att.fileName,
                    url: att.url || null,
                    caption: att.caption || '',
                    isInternal: att.isInternal || false,
                    isDeleted: false
                });
            }
        });

        referencesFromCategory.forEach(ref => {
            if (!ref.url) {
                console.warn("Skipping invalid reference (missing url):", ref);
                return;
            }
            if (!window.references.some(r => r.url === ref.url && r.id === ref.id && !r.isDeleted)) {
                window.references.push({
                    id: isCreateMode ? 0 : (ref.id || 0),
                    url: ref.url,
                    description: ref.description || '',
                    isInternal: ref.isInternal || false,
                    openOption: ref.openOption || '_self',
                    isDeleted: false
                });
            }
        });

        console.log("After adding category data:", {
            attachments: window.attachments,
            references: window.references
        });

        window.reindexAttachments();
        window.reindexReferences();
    };

    // Clear sessionStorage
    window.clearSessionStorage = function () {
        sessionStorage.removeItem('solutionAttachments');
        sessionStorage.removeItem('solutionReferences');
        console.log('Cleared sessionStorage');
    };

    // Handle delete-attachment click with unique listener
    const attachmentList = document.getElementById("attachmentList");
    if (attachmentList) {
        // Remove existing listeners to prevent conflicts
        const newList = attachmentList.cloneNode(true);
        attachmentList.parentNode.replaceChild(newList, attachmentList);
        newList.addEventListener("click", function (e) {
            if (e.target.classList.contains("delete-attachment")) {
                e.preventDefault();
                const index = parseInt(e.target.dataset.index);
                console.log(`Marking attachment for deletion: index=${index}`);
                if (window.attachments[index]) {
                    window.attachments[index].isDeleted = true;
                    window.reindexAttachments();
                } else {
                    console.warn(`Attachment at index ${index} not found`);
                }
            }
        }, { once: false });
    }

    // Handle delete-reference click
    document.getElementById("referenceList").addEventListener("click", function (e) {
        if (e.target.classList.contains("delete-reference")) {
            e.preventDefault();
            const index = parseInt(e.target.dataset.index);
            console.log(`Marking reference for deletion: index=${index}`);
            if (window.references[index]) {
                window.references[index].isDeleted = true;
                window.reindexReferences();
            } else {
                console.warn(`Reference at index ${index} not found`);
            }
        }
    });

    // Initial state restoration
    forceRestoreState();
})();