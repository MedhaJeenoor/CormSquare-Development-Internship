console.log("Script starting: solution-attachments-references.js");

(function () {
    if (typeof jQuery === "undefined") {
        console.error("jQuery is not loaded!");
        return;
    } else {
        console.log("jQuery is loaded!");
    }

    let attachments = [];
    let references = [];

    // Initialize existing attachments and references
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

    // Setup Add Buttons
    const uploadBtn = document.getElementById("uploadAttachmentBtn");
    if (uploadBtn) {
        uploadBtn.addEventListener("click", function (e) {
            e.preventDefault();
            document.getElementById("attachmentInput").click();
        });
    }

    const attachmentInput = document.getElementById("attachmentInput");
    if (attachmentInput) {
        attachmentInput.addEventListener("change", function (event) {
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
    }

    const addRefBtn = document.getElementById("addReferenceBtn");
    if (addRefBtn) {
        addRefBtn.addEventListener("click", function (e) {
            e.preventDefault();
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
    }

    // Event delegation
    document.getElementById("attachmentList").addEventListener("change", function (e) {
        if (e.target.classList.contains("internal-attachment")) {
            e.preventDefault();
            const index = parseInt(e.target.dataset.index);
            if (isNaN(index) || index < 0 || index >= attachments.length) return;
            attachments[index].isInternal = e.target.checked;
            updateAttachmentData();
        }
    });

    document.getElementById("attachmentList").addEventListener("click", function (e) {
        if (e.target.classList.contains("delete-attachment")) {
            e.preventDefault();
            const index = parseInt(e.target.dataset.index);
            if (isNaN(index) || index < 0 || index >= attachments.length) return;
            attachments.splice(index, 1);
            reindexAttachments();
            updateAttachmentData();
        }
    });

    document.getElementById("referenceList").addEventListener("change", function (e) {
        if (e.target.classList.contains("internal-reference")) {
            e.preventDefault();
            const index = parseInt(e.target.dataset.index);
            if (isNaN(index) || index < 0 || index >= references.length) return;
            references[index].isInternal = e.target.checked;
            updateReferenceData();
        }
    });

    document.getElementById("referenceList").addEventListener("click", function (e) {
        if (e.target.classList.contains("delete-reference")) {
            e.preventDefault();
            const index = parseInt(e.target.dataset.index);
            if (isNaN(index) || index < 0 || index >= references.length) return;
            references.splice(index, 1);
            reindexReferences();
            updateReferenceData();
        }
    });

    // Add Attachment to List
    function addAttachmentToList(attachment, index) {
        let attachmentList = document.getElementById("attachmentList");
        let li = document.createElement("li");
        li.id = `attachment-${index}`;
        li.className = "list-group-item d-flex justify-content-between align-items-center";
        li.dataset.attachmentId = attachment.id || 0;
        li.innerHTML = `
            <div>
                <strong>${attachment.fileName}</strong><br />
                <input type="text" class="form-control mt-1 caption-input" placeholder="Enter caption" value="${attachment.caption}" data-index="${index}" />
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
        let referenceList = document.getElementById("referenceList");
        let li = document.createElement("li");
        li.id = `reference-${index}`;
        li.className = "list-group-item d-flex justify-content-between align-items-center";
        li.dataset.referenceId = reference.id || 0;
        li.innerHTML = `
            <div>
                <a href="${reference.url}" target="${reference.openOption}">${reference.url}</a><br />
                <input type="text" class="form-control mt-1 description-input" placeholder="Enter description" value="${reference.description}" data-index="${index}" />
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

    // Add Event Listeners
    function addAttachmentEventListeners(li, index) {
        li.querySelector(".caption-input").addEventListener("input", function () {
            attachments[index].caption = this.value;
            updateAttachmentData();
        });
    }

    function addReferenceEventListeners(li, index) {
        li.querySelector(".description-input").addEventListener("input", function () {
            references[index].description = this.value;
            updateReferenceData();
        });
    }

    // Reindex
    function reindexAttachments() {
        let attachmentList = document.getElementById("attachmentList");
        attachmentList.innerHTML = "";
        attachments.forEach((attachment, index) => addAttachmentToList(attachment, index));
    }

    function reindexReferences() {
        let referenceList = document.getElementById("referenceList");
        referenceList.innerHTML = "";
        references.forEach((reference, index) => addReferenceToList(reference, index));
    }

    // Update Hidden Fields
    function updateAttachmentData() {
        document.getElementById("attachmentData").value = JSON.stringify(attachments.length > 0 ? attachments : []);
    }

    function updateReferenceData() {
        document.getElementById("referenceData").value = JSON.stringify(references.length > 0 ? references : []);
    }

    // Form Submission
    document.querySelector("form").addEventListener("submit", function (e) {
        if (typeof tinymce !== "undefined") {
            tinymce.triggerSave();
            const editorContent = document.getElementById("editor").value;
            document.getElementById("HtmlContent").value = editorContent;
        }
        updateAttachmentData();
        updateReferenceData();
    });

    // Handle category template update
    $(document).on('updateAttachmentsReferences', function (e, templateAttachments, templateReferences) {
        attachments = templateAttachments.map(a => ({
            id: 0,
            fileName: a.fileName,
            caption: a.caption,
            isInternal: a.isInternal
        }));
        references = templateReferences.map(r => ({
            id: 0,
            url: r.url,
            description: r.description,
            isInternal: r.isInternal,
            openOption: r.openOption
        }));
        reindexAttachments();
        reindexReferences();
        updateAttachmentData();
        updateReferenceData();
    });

    // Initialize hidden fields
    updateAttachmentData();
    updateReferenceData();
})();