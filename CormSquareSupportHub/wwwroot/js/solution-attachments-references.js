console.log("Script starting: solution-attachments-references.js (v2.2)");

(function () {
    // Determine if in create mode (no solutionId in URL)
    const urlParams = new URLSearchParams(window.location.search);
    const isCreateMode = !urlParams.has('id'); // Create mode if no 'id' parameter

    // Define updateReferenceData first to ensure availability
    function updateReferenceData() {
        const referenceData = window.references.map(r => ({
            id: r.id || 0,
            url: r.url,
            description: r.description || '',
            isInternal: r.isInternal || false,
            openOption: r.openOption || '_self'
        }));
        const referenceDataInput = document.getElementById("referenceData");
        if (referenceDataInput) {
            referenceDataInput.value = JSON.stringify(referenceData);
            console.log("Updated ReferenceData:", referenceDataInput.value);
        } else {
            console.warn("ReferenceData input not found.");
        }
    }

    // Expose updateReferenceData globally immediately
    window.updateReferenceData = updateReferenceData;

    window.attachments = [];
    window.references = [];
    console.log("Initialized: window.references =", window.references, "isCreateMode =", isCreateMode);

    // Initialize existing solution attachments
    document.querySelectorAll('#attachmentList li').forEach((li, index) => {
        const attachmentId = parseInt(li.dataset.attachmentId) || 0;
        window.attachments.push({
            id: attachmentId,
            fileName: li.querySelector('a').textContent,
            caption: li.querySelector('.caption-input').value,
            isInternal: li.querySelector('.internal-attachment').checked
        });
        addAttachmentEventListeners(li, index);
    });
    console.log("Attachments initialized:", window.attachments);

    // Initialize existing references
    document.querySelectorAll('#referenceList li').forEach((li, index) => {
        const referenceId = parseInt(li.dataset.referenceId) || 0;
        window.references.push({
            id: referenceId,
            url: li.querySelector('a').textContent,
            description: li.querySelector('.description-input').value,
            isInternal: li.querySelector('.internal-reference').checked,
            openOption: li.querySelector('a').getAttribute('target') || '_self'
        });
        addReferenceEventListeners(li, index);
    });
    console.log("References initialized:", window.references);
    updateReferenceData(); // Initialize ReferenceData

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
                if (!window.attachments.some(a => a.fileName === file.name)) {
                    const attachment = {
                        id: 0,
                        fileName: file.name,
                        caption: "",
                        isInternal: false
                    };
                    window.attachments.push(attachment);
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
            const reference = {
                id: 0, // Always 0 for manual references
                url: refUrl,
                description: "",
                isInternal: false,
                openOption: openInNewWindow ? "_blank" : "_self"
            };
            window.references.push(reference);
            addReferenceToList(reference, window.references.length - 1);
            console.log("After adding manual reference:", window.references);
            updateReferenceData();
        });
    }

    // Event delegation for attachments
    document.getElementById("attachmentList").addEventListener("change", function (e) {
        if (e.target.classList.contains("internal-attachment")) {
            const index = parseInt(e.target.dataset.index);
            window.attachments[index].isInternal = e.target.checked;
            updateAttachmentData();
        }
    });

    document.getElementById("attachmentList").addEventListener("click", function (e) {
        if (e.target.classList.contains("delete-attachment")) {
            e.preventDefault();
            const index = parseInt(e.target.dataset.index);
            const attachmentId = window.attachments[index].id;
            if (attachmentId > 0) {
                window.removeAttachment(attachmentId);
            } else {
                window.attachments.splice(index, 1);
                window.reindexAttachments();
                updateAttachmentData();
            }
        }
    });

    // Event delegation for references
    document.getElementById("referenceList").addEventListener("change", function (e) {
        if (e.target.classList.contains("internal-reference")) {
            const index = parseInt(e.target.dataset.index);
            window.references[index].isInternal = e.target.checked;
            console.log("After updating reference internal flag:", window.references);
            updateReferenceData();
        }
    });

    document.getElementById("referenceList").addEventListener("click", function (e) {
        if (e.target.classList.contains("delete-reference")) {
            e.preventDefault();
            const index = parseInt(e.target.dataset.index);
            const referenceId = window.references[index].id;
            if (referenceId > 0 && !isCreateMode) {
                // Only attempt server-side delete in edit mode
                jQuery.post('/Admin/Solution/RemoveReference', { id: referenceId }, function (response) {
                    if (response && response.success) {
                        window.references.splice(index, 1);
                        window.reindexReferences();
                        console.log("After deleting reference:", window.references);
                        updateReferenceData();
                    } else {
                        alert(response ? response.message : "Failed to delete reference.");
                    }
                }).fail(function () {
                    alert("An error occurred while deleting the reference.");
                });
            } else {
                window.references.splice(index, 1);
                window.reindexReferences();
                console.log("After deleting reference:", window.references);
                updateReferenceData();
            }
        }
    });

    function addAttachmentToList(attachment, index) {
        let attachmentList = document.getElementById("attachmentList");
        let li = document.createElement("li");
        li.id = `attachment-${index}`;
        li.className = "list-group-item d-flex justify-content-between align-items-center";
        li.dataset.attachmentId = attachment.id || 0;
        li.innerHTML = `
            <div>
                <strong>${attachment.fileName}</strong><br />
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

    function addReferenceToList(reference, index) {
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
        addReferenceEventListeners(li, index);
        referenceList.appendChild(li);
    }

    function addAttachmentEventListeners(li, index) {
        li.querySelector(".caption-input").addEventListener("input", function () {
            window.attachments[index].caption = this.value;
            updateAttachmentData();
        });
    }

    function addReferenceEventListeners(li, index) {
        li.querySelector(".description-input").addEventListener("input", function () {
            window.references[index].description = this.value;
            console.log("After updating reference description:", window.references);
            updateReferenceData();
        });
    }

    window.reindexAttachments = function () {
        let attachmentList = document.getElementById("attachmentList");
        attachmentList.innerHTML = "";
        window.attachments.forEach((attachment, index) => addAttachmentToList(attachment, index));
    };

    window.reindexReferences = function () {
        let referenceList = document.getElementById("referenceList");
        referenceList.innerHTML = "";
        window.references.forEach((reference, index) => addReferenceToList(reference, index));
    };

    function updateAttachmentData() {
        const attachmentDataInput = document.getElementById("attachmentData");
        if (attachmentDataInput) {
            attachmentDataInput.value = JSON.stringify(window.attachments.map(a => ({
                id: a.id,
                fileName: a.fileName,
                caption: a.caption,
                isInternal: a.isInternal
            })));
            console.log("Updated AttachmentData:", attachmentDataInput.value);
        }
    }

    window.updateAttachmentsAndReferences = function (attachmentsFromCategory, referencesFromCategory) {
        console.log("updateAttachmentsAndReferences called with:", {
            attachments: attachmentsFromCategory,
            references: referencesFromCategory
        });

        // Validate referencesFromCategory
        if (!Array.isArray(referencesFromCategory)) {
            console.warn("referencesFromCategory is not an array:", referencesFromCategory);
            referencesFromCategory = [];
        }

        attachmentsFromCategory.forEach(att => {
            if (!window.attachments.some(a => a.fileName === att.fileName && a.id === att.id)) {
                window.attachments.push({
                    id: att.id || 0,
                    fileName: att.fileName,
                    caption: att.caption || '',
                    isInternal: att.isInternal || false
                });
            }
        });

        referencesFromCategory.forEach(ref => {
            if (!ref.url) {
                console.warn("Skipping invalid reference (missing url):", ref);
                return;
            }
            if (!window.references.some(r => r.url === ref.url && r.id === ref.id)) {
                window.references.push({
                    id: isCreateMode ? 0 : (ref.id || 0), // Force id: 0 in create mode
                    url: ref.url,
                    description: ref.description || '',
                    isInternal: ref.isInternal || false,
                    openOption: ref.openOption || '_self'
                });
            }
        });

        console.log("After adding category data:", {
            attachments: window.attachments,
            references: window.references
        });

        window.reindexAttachments();
        window.reindexReferences();
        updateAttachmentData();
        updateReferenceData();
    };

    updateAttachmentData();
    updateReferenceData();
})();