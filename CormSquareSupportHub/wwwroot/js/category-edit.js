    // Delete Attachment AJAX

    $(document).on("click", ".delete-attachment", function () {
            var attachmentId = $(this).data("id");

    $.ajax({
        url: '/Admin/Category/RemoveAttachment',
    type: 'POST',
    data: {id: attachmentId },
    success: function (response) {
                    if (response.success) {
        alert(response.message);
    location.reload(); // Refresh after deletion 1
                    } else {
        alert("Error: " + response.message);
                    }
                }
            });
        });

    // Delete Reference AJAX
    $(document).on("click", ".delete-reference", function () {
            var referenceId = $(this).data("id");

    $.ajax({
        url: '/Admin/Category/RemoveReference',
    type: 'POST',
    data: {id: referenceId },
    success: function (response) {
                    if (response.success) {
        alert(response.message);
    location.reload(); // Refresh after deletion
                    } else {
        alert("Error: " + response.message);
                    }
                }
            });
        });

    // Upload New Attachments
    $("#uploadAttachmentBtn").click(function () {
        $("#attachmentInput").click();
        });

    $("#attachmentInput").change(function (event) {
            var files = event.target.files;
    for (var i = 0; i < files.length; i++) {
                var fileName = files[i].name;
    $("#attachmentList").append(`
    <li class="list-group-item">${fileName}</li>
    `);
            }
        });

    // Add New References
    $("#addReferenceBtn").click(function () {
            var referenceHtml = `
    <li class="list-group-item d-flex justify-content-between align-items-center">
        <input type="text" class="form-control me-2 reference-url" placeholder="Reference URL" />
        <input type="text" class="form-control me-2 reference-desc" placeholder="Description" />
        <select class="form-select open-option">
            <option value="Same">Open in Same Window</option>
            <option value="New">Open in New Window</option>
        </select>
        <button type="button" class="btn btn-danger btn-sm remove-reference">Remove</button>
    </li>
    `;
    $("#referenceList").append(referenceHtml);
        });

    // Remove New References
    $(document).on("click", ".remove-reference", function () {
        $(this).parent().remove();
        });
</script>