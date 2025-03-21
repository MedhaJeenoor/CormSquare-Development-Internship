document.addEventListener("DOMContentLoaded", function () {
    let attachments = [];
    let references = [];

    // Upload Attachment Button Click
    document.getElementById("uploadAttachmentBtn").addEventListener("click", function () {
        document.getElementById("attachmentInput").click();
    });

    // Handle File Selection
    document.getElementById("attachmentInput").addEventListener("change", function (event) {
        let files = event.target.files;
        if (files.length === 0) return;

        for (let file of files) {
            let attachment = {
                fileName: file.name,
                caption: "",
                internal: false,
            };
            attachments.push(attachment);
            addAttachmentToList(attachment, attachments.length - 1);
        }
        updateAttachmentData();
        //checkAttachmentFallback();
    });

    // Add Reference Button Click
    document.getElementById("addReferenceBtn").addEventListener("click", function () {
        let refUrl = prompt("Enter reference link:");
        if (!refUrl) return;

        let openInNewWindow = confirm("Should this reference open in a new window?");

        let reference = {
            url: refUrl,
            description: "",
            internal: false,
            openOption: openInNewWindow ? "_blank" : "_self",
        };

        references.push(reference);
        addReferenceToList(reference, references.length - 1);
        updateReferenceData();
        //checkReferenceFallback();
    });

    // Add Attachment to List Dynamically
    function addAttachmentToList(attachment, index) {
        let attachmentList = document.getElementById("attachmentList");

        let li = document.createElement("li");
        li.id = `attachment-${index}`;
        li.className = "list-group-item d-flex justify-content-between align-items-center";
        li.innerHTML = `
            <div>
                <strong>${attachment.fileName}</strong> <br />
                <input type="text" class="form-control mt-1 caption-input" placeholder="Enter caption" value="${attachment.caption}" data-index="${index}" />
            </div>
            <div>
                <input type="checkbox" class="form-check-input internal-attachment" data-index="${index}" ${attachment.internal ? "checked" : ""} />
                <span>Internal</span>
                <span class="text-danger delete-attachment ms-3" style="cursor:pointer;" data-index="${index}">❌</span>
            </div>
        `;

        addAttachmentEventListeners(li, index);
        attachmentList.appendChild(li);
    }

    // Add Reference to List Dynamically
    function addReferenceToList(reference, index) {
        let referenceList = document.getElementById("referenceList");

        let li = document.createElement("li");
        li.id = `reference-${index}`;
        li.className = "list-group-item d-flex justify-content-between align-items-center";
        li.innerHTML = `
            <div>
                <a href="${reference.url}" target="${reference.openOption}">${reference.url}</a><br />
                <input type="text" class="form-control mt-1 description-input" placeholder="Enter description" value="${reference.description}" data-index="${index}" />
            </div>
            <div>
                <input type="checkbox" class="form-check-input internal-reference" data-index="${index}" ${reference.internal ? "checked" : ""} />
                <span>Internal</span>
                <span class="text-danger delete-reference ms-3" style="cursor:pointer;" data-index="${index}">❌</span>
            </div>
        `;

        addReferenceEventListeners(li, index);
        referenceList.appendChild(li);
    }

    // Add Event Listeners for Attachments
    function addAttachmentEventListeners(li, index) {
        li.querySelector(".caption-input").addEventListener("input", function () {
            attachments[index].caption = this.value;
            updateAttachmentData();
        });

        li.querySelector(".internal-attachment").addEventListener("change", function () {
            attachments[index].internal = this.checked;
            updateAttachmentData();
        });

        li.querySelector(".delete-attachment").addEventListener("click", function () {
            attachments.splice(index, 1);
            reindexAttachments();
            //checkAttachmentFallback();
            updateAttachmentData();
        });
    }

    // Add Event Listeners for References
    function addReferenceEventListeners(li, index) {
        li.querySelector(".description-input").addEventListener("input", function () {
            references[index].description = this.value;
            updateReferenceData();
        });

        li.querySelector(".internal-reference").addEventListener("change", function () {
            references[index].internal = this.checked;
            updateReferenceData();
        });

        li.querySelector(".delete-reference").addEventListener("click", function () {
            references.splice(index, 1);
            reindexReferences();
            //checkReferenceFallback();
            updateReferenceData();
        });
    }

    // Reindex Attachments after Deletion
    function reindexAttachments() {
        let attachmentList = document.getElementById("attachmentList");
        attachmentList.innerHTML = "";
        attachments.forEach((attachment, index) => {
            addAttachmentToList(attachment, index);
        });
    }

    // Reindex References after Deletion
    function reindexReferences() {
        let referenceList = document.getElementById("referenceList");
        referenceList.innerHTML = "";
        references.forEach((reference, index) => {
            addReferenceToList(reference, index);
        });
    }

    // Add fallback message if attachments list is empty
    //function checkAttachmentFallback() {
    //    let attachmentList = document.getElementById("attachmentList");
    //    if (attachments.length === 0) {
    //        attachmentList.innerHTML = "";
    //        //attachmentList.innerHTML = '<li class="list-group-item text-muted">No attachments added.</li>';
    //    }
    //}

    //// Add fallback message if references list is empty
    //function checkReferenceFallback() {
    //    let referenceList = document.getElementById("referenceList");
    //    if (references.length === 0) {
    //        referenceList.innerHTML = ""; // Clear list if empty
    //        //referenceList.innerHTML = '<li class="list-group-item text-muted">No references added.</li>';
    //    }
    //}

    // Update Hidden Fields with JSON Data
    function updateAttachmentData() {
        document.getElementById("attachmentData").value = JSON.stringify(attachments);
    }

    function updateReferenceData() {
        document.getElementById("referenceData").value = JSON.stringify(references);
    }

    // Ensure data is updated correctly before form submission
    document.querySelector("form").addEventListener("submit", function () {
        updateAttachmentData();
        updateReferenceData();
    });

    // Initial fallback messages on page load
    //checkAttachmentFallback();
    //checkReferenceFallback();
});



//document.addEventListener("DOMContentLoaded", function () {
//    let attachments = [];
//    let references = [];

//    // Upload Attachment Button Click
//    document.getElementById("uploadAttachmentBtn").addEventListener("click", function () {
//        document.getElementById("attachmentInput").click();
//    });

//    // Handle File Selection
//    document.getElementById("attachmentInput").addEventListener("change", function (event) {
//        let files = event.target.files;
//        if (files.length === 0) return;

//        for (let file of files) {
//            let attachment = {
//                fileName: file.name,
//                caption: "",
//                internal: false,
//            };
//            attachments.push(attachment);
//            addAttachmentToList(attachment, attachments.length - 1);
//        }
//        updateAttachmentData();
//    });

//    // Add Reference Button Click
//    document.getElementById("addReferenceBtn").addEventListener("click", function () {
//        let refUrl = prompt("Enter reference link:");
//        if (!refUrl) return;

//        let openInNewWindow = confirm("Should this reference open in a new window?");

//        let reference = {
//            url: refUrl,
//            description: "",
//            internal: false,
//            openOption: openInNewWindow ? "_blank" : "_self"
//        };

//        references.push(reference);
//        addReferenceToList(reference, references.length - 1);
//        updateReferenceData();
//    });

//    // Add Attachment to List Dynamically
//    function addAttachmentToList(attachment, index) {
//        let attachmentList = document.getElementById("attachmentList");

//        let li = document.createElement("li");
//        li.id = `attachment-${index}`;
//        li.className = "list-group-item d-flex justify-content-between align-items-center";
//        li.innerHTML = `
//            <div>
//                <strong>${attachment.fileName}</strong> <br />
//                <input type="text" class="form-control mt-1 caption-input" placeholder="Enter caption" value="${attachment.caption}" data-index="${index}" />
//            </div>
//            <div>
//                <input type="checkbox" class="form-check-input internal-attachment" data-index="${index}" ${attachment.internal ? "checked" : ""} />
//                <span>Internal</span>
//                <span class="text-danger delete-attachment ms-3" style="cursor:pointer;" data-index="${index}">❌</span>
//            </div>
//        `;

//        addAttachmentEventListeners(li, index);
//        attachmentList.appendChild(li);
//    }

//    // Add Reference to List Dynamically
//    function addReferenceToList(reference, index) {
//        let referenceList = document.getElementById("referenceList");

//        let li = document.createElement("li");
//        li.id = `reference-${index}`;
//        li.className = "list-group-item d-flex justify-content-between align-items-center";
//        li.innerHTML = `
//            <div>
//                <a href="${reference.url}" target="${reference.openOption}">${reference.url}</a><br />
//                <input type="text" class="form-control mt-1 description-input" placeholder="Enter description" value="${reference.description}" data-index="${index}" />
//            </div>
//            <div>
//                <input type="checkbox" class="form-check-input internal-reference" data-index="${index}" ${reference.internal ? "checked" : ""} />
//                <span>Internal</span>
//                <span class="text-danger delete-reference ms-3" style="cursor:pointer;" data-index="${index}">❌</span>
//            </div>
//        `;

//        addReferenceEventListeners(li, index);
//        referenceList.appendChild(li);
//    }

//    // Add Event Listeners for Attachments
//    function addAttachmentEventListeners(li, index) {
//        li.querySelector(".caption-input").addEventListener("input", function () {
//            attachments[index].caption = this.value;
//            updateAttachmentData();
//        });

//        li.querySelector(".internal-attachment").addEventListener("change", function () {
//            attachments[index].internal = this.checked;
//            updateAttachmentData();
//        });

//        li.querySelector(".delete-attachment").addEventListener("click", function () {
//            attachments.splice(index, 1);
//            document.getElementById(`attachment-${index}`).remove();
//            updateAttachmentData();
//        });
//    }

//    // Add Event Listeners for References
//    function addReferenceEventListeners(li, index) {
//        li.querySelector(".description-input").addEventListener("input", function () {
//            references[index].description = this.value;
//            updateReferenceData();
//        });

//        li.querySelector(".internal-reference").addEventListener("change", function () {
//            references[index].internal = this.checked;
//            updateReferenceData();
//        });

//        li.querySelector(".delete-reference").addEventListener("click", function () {
//            references.splice(index, 1);
//            document.getElementById(`reference-${index}`).remove();
//            updateReferenceData();
//        });
//    }

//    // Update Hidden Fields with JSON Data
//    function updateAttachmentData() {
//        document.getElementById("attachmentData").value = JSON.stringify(attachments);
//    }

//    function updateReferenceData() {
//        document.getElementById("referenceData").value = JSON.stringify(references);
//    }

//    // Ensure data is updated correctly before form submission
//    document.querySelector("form").addEventListener("submit", function () {
//        updateAttachmentData();
//        updateReferenceData();
//    });
//});



//document.addEventListener("DOMContentLoaded", function () {
//    let attachments = [];
//    let references = [];

//    // Upload Attachment Button Click
//    document.getElementById("uploadAttachmentBtn").addEventListener("click", function () {
//        document.getElementById("attachmentInput").click();
//    });

//    // Handle File Selection
//    document.getElementById("attachmentInput").addEventListener("change", function (event) {
//        let files = event.target.files;
//        if (files.length === 0) return;

//        for (let file of files) {
//            let attachment = {
//                fileName: file.name,
//                caption: "",
//                internal: false, // Default unchecked
//            };
//            attachments.push(attachment);
//            addAttachmentToList(attachment, attachments.length - 1);
//        }
//        updateAttachmentData(); // Update after adding
//    });

//    // Add Reference Button Click
//    document.getElementById("addReferenceBtn").addEventListener("click", function () {
//        let refUrl = prompt("Enter reference link:");
//        if (!refUrl) return;

//        let openInNewWindow = confirm("Should this reference open in a new window?");

//        let reference = {
//            url: refUrl,
//            description: "",
//            internal: false, // Default unchecked
//            openOption: openInNewWindow ? "_blank" : "_self"
//        };

//        references.push(reference);
//        addReferenceToList(reference, references.length - 1);
//        updateReferenceData(); // Update after adding
//    });

//    // Add Attachment to List Dynamically
//    function addAttachmentToList(attachment, index) {
//        let attachmentList = document.getElementById("attachmentList");

//        let li = document.createElement("li");
//        li.id = `attachment-${index}`;
//        li.className = "list-group-item d-flex justify-content-between align-items-center";
//        li.innerHTML = `
//            <div>
//                <strong>${attachment.fileName}</strong> <br />
//                <input type="text" class="form-control mt-1 caption-input" placeholder="Enter caption" value="${attachment.caption}" data-index="${index}" />
//            </div>
//            <div>
//                <input type="checkbox" class="form-check-input internal-attachment" data-index="${index}" ${attachment.internal ? "checked" : ""} />
//                <span>Internal</span>
//                <span class="text-danger delete-attachment ms-3" style="cursor:pointer;" data-index="${index}">❌</span>
//            </div>
//        `;

//        addAttachmentEventListeners(li, index);
//        attachmentList.appendChild(li);
//    }

//    // Add Reference to List Dynamically
//    function addReferenceToList(reference, index) {
//        let referenceList = document.getElementById("referenceList");

//        let li = document.createElement("li");
//        li.id = `reference-${index}`;
//        li.className = "list-group-item d-flex justify-content-between align-items-center";
//        li.innerHTML = `
//            <div>
//                <a href="${reference.url}" target="${reference.openOption}">${reference.url}</a><br />
//                <input type="text" class="form-control mt-1 description-input" placeholder="Enter description" value="${reference.description}" data-index="${index}" />
//            </div>
//            <div>
//                <input type="checkbox" class="form-check-input internal-reference" data-index="${index}" ${reference.internal ? "checked" : ""} />
//                <span>Internal</span>
//                <span class="text-danger delete-reference ms-3" style="cursor:pointer;" data-index="${index}">❌</span>
//            </div>
//        `;

//        addReferenceEventListeners(li, index);
//        referenceList.appendChild(li);
//    }

//    // Add Event Listeners for Attachments
//    function addAttachmentEventListeners(li, index) {
//        li.querySelector(".caption-input").addEventListener("input", function () {
//            attachments[index].caption = this.value;
//            updateAttachmentData();
//        });

//        li.querySelector(".internal-attachment").addEventListener("change", function () {
//            attachments[index].internal = this.checked;
//            updateAttachmentData();
//        });

//        li.querySelector(".delete-attachment").addEventListener("click", function () {
//            attachments.splice(index, 1);
//            document.getElementById(`attachment-${index}`).remove();
//            updateAttachmentData();
//        });
//    }

//    // Add Event Listeners for References
//    function addReferenceEventListeners(li, index) {
//        li.querySelector(".description-input").addEventListener("input", function () {
//            references[index].description = this.value;
//            updateReferenceData();
//        });

//        li.querySelector(".internal-reference").addEventListener("change", function () {
//            references[index].internal = this.checked;
//            updateReferenceData();
//        });

//        li.querySelector(".delete-reference").addEventListener("click", function () {
//            references.splice(index, 1);
//            document.getElementById(`reference-${index}`).remove();
//            updateReferenceData();
//        });
//    }

//    // Update Hidden Fields with JSON Data
//    function updateAttachmentData() {
//        document.getElementById("attachmentData").value = JSON.stringify(attachments);
//    }

//    function updateReferenceData() {
//        document.getElementById("referenceData").value = JSON.stringify(references);
//    }

//    // Ensure data is updated correctly before form submission
//    document.querySelector("form").addEventListener("submit", function () {
//        updateAttachmentData();
//        updateReferenceData();
//    });
//});
