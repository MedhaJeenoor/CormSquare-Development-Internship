console.log("Script starting: solution-attachments-references.js");

(function () {
    console.log("Immediate execution - initializing solution-attachments-references.js");

    if (typeof jQuery === "undefined") {
        console.error("jQuery is not loaded!");
        return;
    } else {
        console.log("jQuery is loaded!");
    }

    let attachments = [];
    let references = [];

    console.log("Initializing existing attachments...");
    document.querySelectorAll('#attachmentList li').forEach((li, index) => {
        const attachmentId = parseInt(li.dataset.attachmentId);
        if (attachmentId) {
            attachments.push({
                id: attachmentId,
                fileName: li.querySelector('strong').textContent,
                caption: li.querySelector('.caption-input').value,
                isInternal: li.querySelector('.internal-attachment').checked
            });
            addAttachmentEventListeners(li, index);
        }
    });
    console.log("Attachments initialized:", attachments);

    console.log("Initializing existing references...");
    document.querySelectorAll('#referenceList li').forEach((li, index) => {
        const referenceId = parseInt(li.dataset.referenceId);
        if (referenceId) {
            references.push({
                id: referenceId,
                url: li.querySelector('a').textContent,
                description: li.querySelector('.description-input').value,
                isInternal: li.querySelector('.internal-reference').checked,
                openOption: li.querySelector('a').getAttribute('target')
            });
            addReferenceEventListeners(li, index);
        }
    });
    console.log("References initialized:", references);

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
                addAttachmentToList(attachment, attachments.length - 1);
            }
            updateAttachmentData();
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
            addReferenceToList(reference, references.length - 1);
            updateReferenceData();
        });
    } else {
        console.error("addReferenceBtn not found!");
    }

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
                jQuery.post('/Admin/Solution/RemoveAttachment', { id: attachmentId }, function (response) {
                    console.log("RemoveAttachment response:", response);
                    if (response && response.success) {
                        attachments.splice(index, 1);
                        reindexAttachments();
                        updateAttachmentData();
                    } else {
                        alert(response ? response.message : "Failed to delete attachment.");
                    }
                }).fail(function (xhr, status, error) {
                    console.error("Error deleting attachment:", error);
                    alert("An error occurred while deleting the attachment.");
                });
            } else {
                attachments.splice(index, 1);
                reindexAttachments();
                updateAttachmentData();
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
                jQuery.post('/Admin/Solution/RemoveReference', { id: referenceId }, function (response) {
                    console.log("RemoveReference response:", response);
                    if (response && response.success) {
                        references.splice(index, 1);
                        reindexReferences();
                        updateReferenceData();
                    } else {
                        alert(response ? response.message : "Failed to delete reference.");
                    }
                }).fail(function (xhr, status, error) {
                    console.error("Error deleting reference:", error);
                    alert("An error occurred while deleting the reference.");
                });
            } else {
                references.splice(index, 1);
                reindexReferences();
                updateReferenceData();
            }
        }
    });

    function addAttachmentToList(attachment, index) {
        console.log("Adding attachment to list at index:", index);
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
        updateAttachmentData();
    }

    function addReferenceToList(reference, index) {
        console.log("Adding reference to list at index:", index);
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
        updateReferenceData();
    }

    function addAttachmentEventListeners(li, index) {
        console.log("Adding event listeners for attachment at index:", index);
        li.querySelector(".caption-input").addEventListener("input", function () {
            console.log("Caption input changed for attachment at index:", index);
            attachments[index].caption = this.value;
            updateAttachmentData();
        });
    }

    function addReferenceEventListeners(li, index) {
        console.log("Adding event listeners for reference at index:", index);
        li.querySelector(".description-input").addEventListener("input", function () {
            console.log("Description input changed for reference at index:", index);
            references[index].description = this.value;
            updateReferenceData();
        });
        li.querySelector(".internal-reference").addEventListener("change", function () {
            console.log("Internal changed for reference at index:", index);
            references[index].isInternal = this.checked;
            updateReferenceData();
        });
    }

    function reindexAttachments() {
        console.log("Reindexing attachments...");
        let attachmentList = document.getElementById("attachmentList");
        attachmentList.innerHTML = "";
        attachments.forEach((attachment, index) => addAttachmentToList(attachment, index));
    }

    function reindexReferences() {
        console.log("Reindexing references...");
        let referenceList = document.getElementById("referenceList");
        referenceList.innerHTML = "";
        references.forEach((reference, index) => addReferenceToList(reference, index));
    }

    function updateAttachmentData() {
        var json = JSON.stringify(attachments.length > 0 ? attachments : []);
        document.getElementById("attachmentData").value = json;
        console.log("Updated attachmentData:", json);
    }

    function updateReferenceData() {
        var json = JSON.stringify(references.length > 0 ? references : []);
        document.getElementById("referenceData").value = json;
        console.log("Updated referenceData:", json);
    }

    document.querySelector("#solutionForm").addEventListener("submit", function (e) {
        console.log("Form submitting...");
        if (typeof tinymce !== "undefined") {
            tinymce.triggerSave();
            const editorContent = document.getElementById("editor").value;
            document.getElementById("HtmlContent").value = editorContent;
            console.log("TinyMCE content saved:", editorContent);
        }
        updateAttachmentData();
        updateReferenceData();
        console.log("Files to be submitted:", document.getElementById("attachmentInput").files);
    });

    window.updateAttachmentsAndReferences = function (attachmentsFromCategory, referencesFromCategory) {
        console.log("Received updateAttachmentsAndReferences call with:", attachmentsFromCategory, referencesFromCategory);

        attachments = attachments.filter(att => att.id > 0);
        references = references.filter(ref => ref.id > 0);

        attachmentsFromCategory.forEach(att => {
            if (!attachments.some(a => a.fileName === att.fileName && a.id > 0)) {
                attachments.push({
                    id: 0,
                    fileName: att.fileName,
                    caption: att.caption || '',
                    isInternal: att.isInternal || false
                });
            }
        });

        referencesFromCategory.forEach(ref => {
            if (!references.some(r => r.url === ref.url && r.id > 0)) {
                references.push({
                    id: 0,
                    url: ref.url,
                    description: ref.description || '',
                    isInternal: ref.isInternal || false,
                    openOption: ref.openOption || '_self'
                });
            }
        });

        reindexAttachments();
        reindexReferences();
        updateAttachmentData();
        updateReferenceData();
    };

    $(document).on('updateAttachmentsReferences', function (e, attachmentsFromCategory, referencesFromCategory) {
        console.log("Received updateAttachmentsReferences event with:", attachmentsFromCategory, referencesFromCategory);
        window.updateAttachmentsAndReferences(attachmentsFromCategory, referencesFromCategory);
    });

    console.log("Initializing hidden fields...");
    updateAttachmentData();
    updateReferenceData();

    console.log("Script initialization complete!");
})();