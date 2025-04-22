console.log("Script starting: category-attachments-references.js");

(function () {
    console.log("Immediate execution - initializing category-attachments-references.js");

    if (typeof jQuery === "undefined") {
        console.error("jQuery is not loaded!");
        alert("Error: jQuery is not loaded. Please check script references.");
        return;
    } else {
        console.log("jQuery is loaded!");
    }

    let attachments = [];
    let references = [];
    let deletedAttachmentIds = [];
    let deletedReferenceIds = [];
    let pendingFiles = [];
    const isEditView = document.querySelector('form[action*="/Edit"]') !== null;
    const isCreateView = document.querySelector('form[action*="/Create"]') !== null;

    // Get category ID from hidden input or use fallback for Create
    function getCategoryId() {
        const id = document.querySelector('input[name="Id"]')?.value;
        if (isEditView && !id) {
            console.error("Category ID not found in Edit view!");
            alert("Error: Category ID not found in Edit view.");
        }
        return id ? id : (isCreateView ? "create_temp" : null);
    }

    // Initialize state from hidden inputs and sessionStorage
    function initializeState() {
        console.log("Initializing state...");
        const categoryId = getCategoryId();
        if (!categoryId) {
            console.warn("No categoryId found, skipping initialization.");
            alert("Error: Category ID not found. Changes may not persist.");
            return;
        }

        // 1. Initialize from hidden inputs (form state)
        const referenceDataInput = document.getElementById("referenceData");
        if (referenceDataInput && referenceDataInput.value) {
            try {
                references = JSON.parse(referenceDataInput.value);
                console.log("Restored references from hidden input:", JSON.stringify(references, null, 2));
            } catch (e) {
                console.error("Failed to parse referenceData from hidden input:", e);
                references = [];
            }
        }

        const attachmentDataInput = document.getElementById("attachmentData");
        if (attachmentDataInput && attachmentDataInput.value) {
            try {
                attachments = JSON.parse(attachmentDataInput.value);
                console.log("Restored attachments from hidden input:", JSON.stringify(attachments, null, 2));
            } catch (e) {
                console.error("Failed to parse attachmentData from hidden input:", e);
                attachments = [];
            }
        }

        // 2. Fallback to sessionStorage if hidden inputs are empty
        const storedReferences = sessionStorage.getItem(`references_${categoryId}`);
        if (storedReferences && references.length === 0) {
            try {
                references = JSON.parse(storedReferences);
                console.log("Restored references from sessionStorage:", JSON.stringify(references, null, 2));
                if (references.some(r => r.id === 0)) {
                    console.log("Found new references (id: 0):", JSON.stringify(references.filter(r => r.id === 0), null, 2));
                }
            } catch (e) {
                console.error("Failed to parse references from sessionStorage:", e);
                references = [];
            }
        }

        const storedAttachments = sessionStorage.getItem(`attachments_${categoryId}`);
        if (storedAttachments && attachments.length === 0) {
            try {
                attachments = JSON.parse(storedAttachments);
                console.log("Restored attachments from sessionStorage:", JSON.stringify(attachments, null, 2));
            } catch (e) {
                console.error("Failed to parse attachments from sessionStorage:", e);
                attachments = [];
            }
        }

        const storedPendingFiles = sessionStorage.getItem(`pendingFiles_${categoryId}`);
        if (storedPendingFiles) {
            try {
                pendingFiles = JSON.parse(storedPendingFiles);
                console.log("Restored pending files from sessionStorage:", JSON.stringify(pendingFiles, null, 2));
            } catch (e) {
                console.error("Failed to parse pending files from sessionStorage:", e);
                pendingFiles = [];
            }
        }

        // 3. Initialize existing data from DOM (Edit view only)
        if (isEditView) {
            console.log("Initializing existing attachments from DOM...");
            document.querySelectorAll('#attachmentList li').forEach((li) => {
                const attachmentId = parseInt(li.dataset.attachmentId);
                if (attachmentId && !attachments.some(a => a.id === attachmentId)) {
                    const attachment = {
                        id: attachmentId,
                        fileName: li.querySelector('strong a')?.textContent || li.querySelector('strong').textContent,
                        caption: li.querySelector('.caption-input').value || "",
                        isInternal: li.querySelector('.internal-attachment').checked
                    };
                    attachments.push(attachment);
                }
            });
            console.log("DOM attachments initialized:", JSON.stringify(attachments, null, 2));

            console.log("Initializing existing references from DOM...");
            document.querySelectorAll('#referenceList li').forEach((li) => {
                const referenceId = parseInt(li.dataset.referenceId);
                if (referenceId && !references.some(r => r.id === referenceId)) {
                    const reference = {
                        id: referenceId,
                        url: li.querySelector('a').textContent,
                        description: li.querySelector('.description-input').value || "",
                        isInternal: li.querySelector('.internal-reference').checked,
                        openOption: li.querySelector('a').getAttribute('target') || "_self"
                    };
                    references.push(reference);
                }
            });
            console.log("DOM references initialized:", JSON.stringify(references, null, 2));
        }
    }

    // Save to sessionStorage (fallback)
    function saveToSessionStorage() {
        const categoryId = getCategoryId();
        if (!categoryId) {
            console.warn("No categoryId found, skipping sessionStorage save.");
            return;
        }

        try {
            sessionStorage.setItem(`attachments_${categoryId}`, JSON.stringify(attachments));
            sessionStorage.setItem(`references_${categoryId}`, JSON.stringify(references));
            sessionStorage.setItem(`pendingFiles_${categoryId}`, JSON.stringify(pendingFiles));
            console.log(`Saved to sessionStorage for key: ${categoryId}`, {
                attachments: JSON.stringify(attachments, null, 2),
                references: JSON.stringify(references, null, 2),
                pendingFiles: JSON.stringify(pendingFiles, null, 2)
            });
        } catch (e) {
            console.error("Failed to save to sessionStorage:", e);
        }
    }

    // Clear sessionStorage
    function clearSessionStorage() {
        const categoryId = getCategoryId();
        if (!categoryId) {
            console.warn("No categoryId found, skipping sessionStorage clear.");
            return;
        }

        sessionStorage.removeItem(`attachments_${categoryId}`);
        sessionStorage.removeItem(`references_${categoryId}`);
        sessionStorage.removeItem(`pendingFiles_${categoryId}`);
        console.log(`Cleared sessionStorage for key: ${categoryId}`);
    }

    // Update hidden inputs
    function updateAttachmentData() {
        const attachmentDataInput = document.getElementById("attachmentData");
        if (attachmentDataInput) {
            attachmentDataInput.value = JSON.stringify(attachments.length > 0 ? attachments : []);
            console.log("Updated attachmentData:", attachmentDataInput.value);
        } else {
            console.error("attachmentData input not found!");
        }
    }

    function updateReferenceData() {
        const referenceDataInput = document.getElementById("referenceData");
        if (referenceDataInput) {
            referenceDataInput.value = JSON.stringify(references.length > 0 ? references : []);
            console.log("Updated referenceData:", referenceDataInput.value);
        } else {
            console.error("referenceData input not found!");
        }
    }

    // Initialize state first
    console.log("Calling initializeState...");
    initializeState();

    // Reindex after initialization
    console.log("Calling reindexAttachments and reindexReferences...");
    reindexAttachments();
    reindexReferences();

    // Setup Add Buttons
    console.log("Setting up uploadAttachmentBtn listener...");
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

    console.log("Setting up attachmentInput listener...");
    const attachmentInput = document.getElementById("attachmentInput");
    if (attachmentInput) {
        attachmentInput.addEventListener("change", function (event) {
            console.log("attachmentInput changed");
            const files = event.target.files;
            if (!files || files.length === 0) return;

            for (let file of files) {
                const attachment = {
                    id: 0,
                    fileName: file.name,
                    caption: "",
                    isInternal: false
                };
                attachments.push(attachment);
                pendingFiles.push({
                    name: file.name,
                    type: file.type,
                    size: file.size
                });
                addAttachmentToList(attachment, attachments.length - 1);
            }
            updateAttachmentData();
            saveToSessionStorage();
        });
    } else {
        console.error("attachmentInput not found!");
    }

    console.log("Setting up addReferenceBtn listener...");
    const addRefBtn = document.getElementById("addReferenceBtn");
    if (addRefBtn) {
        addRefBtn.addEventListener("click", function (e) {
            e.preventDefault();
            console.log("addReferenceBtn clicked");
            const refUrl = prompt("Enter reference link:");
            if (!refUrl) return;

            const openInNewWindow = confirm("Should this reference open in a new window?");
            const reference = {
                id: 0,
                url: refUrl,
                description: "",
                isInternal: false,
                openOption: openInNewWindow ? "_blank" : "_self"
            };
            references.push(reference);
            console.log("Added new reference:", JSON.stringify(reference, null, 2));
            addReferenceToList(reference, references.length - 1);
            updateReferenceData();
            saveToSessionStorage();
        });
    } else {
        console.error("addReferenceBtn not found!");
    }

    // Event delegation for all elements
    console.log("Setting up event delegation...");
    document.getElementById("attachmentList").addEventListener("change", function (e) {
        if (e.target.classList.contains("internal-attachment")) {
            e.preventDefault();
            const index = parseInt(e.target.dataset.index);
            if (isNaN(index) || index < 0 || index >= attachments.length) {
                console.error("Invalid index for internal-attachment:", index);
                return;
            }
            console.log("Attachment IsInternal changed for index", index, "to", e.target.checked);
            attachments[index].isInternal = e.target.checked;
            updateAttachmentData();
            saveToSessionStorage();
        }
    });

    document.getElementById("attachmentList").addEventListener("click", function (e) {
        if (e.target.classList.contains("delete-attachment")) {
            e.preventDefault();
            const index = parseInt(e.target.dataset.index);
            if (isNaN(index) || index < 0 || index >= attachments.length) {
                console.error("Invalid index for delete-attachment:", index);
                return;
            }
            console.log("Delete clicked for attachment at index:", index);
            const attachmentId = attachments[index].id;
            if (attachmentId > 0) {
                jQuery.post('/Admin/Category/RemoveAttachment', { id: attachmentId }, function (response) {
                    console.log("RemoveAttachment response:", response);
                    if (response && response.success) {
                        deletedAttachmentIds.push(attachmentId);
                        attachments.splice(index, 1);
                        pendingFiles.splice(index, 1);
                        reindexAttachments();
                        updateAttachmentData();
                        saveToSessionStorage();
                    } else {
                        alert(response ? response.message : "Failed to mark attachment for deletion.");
                    }
                }).fail(function (xhr, status, error) {
                    console.error("Error marking attachment for deletion:", error);
                    alert("An error occurred while marking the attachment for deletion.");
                });
            } else {
                attachments.splice(index, 1);
                pendingFiles.splice(index, 1);
                reindexAttachments();
                updateAttachmentData();
                saveToSessionStorage();
            }
        }
    });

    document.getElementById("referenceList").addEventListener("change", function (e) {
        if (e.target.classList.contains("internal-reference")) {
            e.preventDefault();
            const index = parseInt(e.target.dataset.index);
            if (isNaN(index) || index < 0 || index >= references.length) {
                console.error("Invalid index for internal-reference:", index);
                return;
            }
            console.log("Reference IsInternal changed for index", index, "to", e.target.checked);
            references[index].isInternal = e.target.checked;
            updateReferenceData();
            saveToSessionStorage();
        }
    });

    document.getElementById("referenceList").addEventListener("click", function (e) {
        if (e.target.classList.contains("delete-reference")) {
            e.preventDefault();
            const index = parseInt(e.target.dataset.index);
            if (isNaN(index) || index < 0 || index >= references.length) {
                console.error("Invalid index for delete-reference:", index);
                return;
            }
            console.log("Delete clicked for reference at index:", index);
            const referenceId = references[index].id;
            if (referenceId > 0) {
                jQuery.post('/Admin/Category/RemoveReference', { id: referenceId }, function (response) {
                    console.log("RemoveReference response:", response);
                    if (response && response.success) {
                        deletedReferenceIds.push(referenceId);
                        references.splice(index, 1);
                        reindexReferences();
                        updateReferenceData();
                        saveToSessionStorage();
                    } else {
                        alert(response ? response.message : "Failed to mark reference for deletion.");
                    }
                }).fail(function (xhr, status, error) {
                    console.error("Error marking reference for deletion:", error);
                    alert("An error occurred while marking the reference for deletion.");
                });
            } else {
                references.splice(index, 1);
                reindexReferences();
                updateReferenceData();
                saveToSessionStorage();
            }
        }
    });

    // Add Attachment to List
    function addAttachmentToList(attachment, index) {
        console.log("Adding attachment to list at index:", index, JSON.stringify(attachment, null, 2));
        let attachmentList = document.getElementById("attachmentList");
        if (!attachmentList) {
            console.error("attachmentList element not found!");
            return;
        }
        let li = document.createElement("li");
        li.id = `attachment-${index}`;
        li.className = "list-group-item d-flex justify-content-between align-items-center";
        li.dataset.attachmentId = attachment.id || 0;

        const isExisting = attachment.id > 0;
        const fileNameHtml = isExisting
            ? `<a href="/Admin/Category/DownloadAttachment?id=${attachment.id}" style="text-decoration: underline;" class="attachment-link">${attachment.fileName}</a>`
            : `<span>${attachment.fileName} ${attachment.id === 0 ? "(Please re-select file before saving)" : ""}</span>`;

        li.innerHTML = `
            <div>
                <strong>${fileNameHtml}</strong><br />
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
    }

    // Add Reference to List
    function addReferenceToList(reference, index) {
        console.log("Adding reference to list at index:", index, JSON.stringify(reference, null, 2));
        let referenceList = document.getElementById("referenceList");
        if (!referenceList) {
            console.error("referenceList element not found!");
            return;
        }
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
        addReferenceEventListeners(li, index);
        referenceList.appendChild(li);
    }

    // Add Event Listeners for Attachments
    function addAttachmentEventListeners(li, index) {
        console.log("Adding event listeners for attachment at index:", index);
        li.querySelector(".caption-input").addEventListener("input", function () {
            console.log("Caption input changed for attachment at index:", index);
            attachments[index].caption = this.value;
            updateAttachmentData();
            saveToSessionStorage();
        });

        li.querySelector(".internal-attachment").addEventListener("change", function (e) {
            e.preventDefault();
            console.log("Attachment IsInternal changed for index", index, "to", this.checked);
            attachments[index].isInternal = this.checked;
            updateAttachmentData();
            saveToSessionStorage();
        });
    }

    // Add Event Listeners for References
    function addReferenceEventListeners(li, index) {
        console.log("Adding event listeners for reference at index:", index);
        li.querySelector(".description-input").addEventListener("input", function () {
            console.log("Description input changed for reference at index:", index);
            references[index].description = this.value;
            updateReferenceData();
            saveToSessionStorage();
        });

        li.querySelector(".internal-reference").addEventListener("change", function (e) {
            e.preventDefault();
            console.log("Reference IsInternal changed for index", index, "to", this.checked);
            references[index].isInternal = this.checked;
            updateReferenceData();
            saveToSessionStorage();
        });
    }

    // Reindex Attachments
    function reindexAttachments() {
        console.log("Reindexing attachments...", JSON.stringify(attachments, null, 2));
        let attachmentList = document.getElementById("attachmentList");
        if (!attachmentList) {
            console.error("attachmentList element not found!");
            return;
        }
        attachmentList.innerHTML = "";
        attachments.forEach((attachment, index) => addAttachmentToList(attachment, index));
    }

    // Reindex References
    function reindexReferences() {
        console.log("Reindexing references...", JSON.stringify(references, null, 2));
        let referenceList = document.getElementById("referenceList");
        if (!referenceList) {
            console.error("referenceList element not found!");
            return;
        }
        referenceList.innerHTML = "";
        references.forEach((reference, index) => addReferenceToList(reference, index));
    }

    // Form Submission with TinyMCE sync and deleted IDs
    document.querySelector("form").addEventListener("submit", function (e) {
        console.log("Form submitting...");
        if (typeof tinymce !== "undefined") {
            tinymce.triggerSave();
            const editorContent = document.getElementById("editor").value;
            document.getElementById("HtmlContent").value = editorContent;
            console.log("TinyMCE content saved:", editorContent);
        }
        updateAttachmentData();
        updateReferenceData();

        // Check if new attachments need re-selection
        const newAttachments = attachments.filter(a => a.id === 0);
        if (newAttachments.length > 0 && document.getElementById("attachmentInput").files.length === 0) {
            alert("Please re-select files for new attachments before saving.");
            e.preventDefault();
            return;
        }

        // Add hidden inputs for deleted IDs
        deletedAttachmentIds.forEach(id => {
            const input = document.createElement("input");
            input.type = "hidden";
            input.name = "deletedAttachmentIds";
            input.value = id;
            this.appendChild(input);
        });

        deletedReferenceIds.forEach(id => {
            const input = document.createElement("input");
            input.type = "hidden";
            input.name = "deletedReferenceIds";
            input.value = id;
            this.appendChild(input);
        });

        console.log("Deleted Attachment IDs:", deletedAttachmentIds);
        console.log("Deleted Reference IDs:", deletedReferenceIds);
        console.log("Files to be submitted:", document.getElementById("attachmentInput").files);

        // Handle form submission via AJAX to clear sessionStorage on success
        e.preventDefault();
        const form = this;
        const formData = new FormData(form);

        jQuery.ajax({
            url: form.action,
            type: form.method,
            data: formData,
            processData: false,
            contentType: false,
            success: function (response) {
                console.log("Form submission response:", response);
                if (response.success) {
                    clearSessionStorage();
                    window.location.href = response.redirectUrl;
                } else {
                    alert(response.message || "Failed to submit form.");
                }
            },
            error: function (xhr, status, error) {
                console.error("Error submitting form:", error);
                alert("An error occurred while submitting the form.");
            }
        });
    });

    // Initialize hidden fields
    console.log("Initializing hidden fields...");
    updateAttachmentData();
    updateReferenceData();

    console.log("Script initialization complete!");
})();