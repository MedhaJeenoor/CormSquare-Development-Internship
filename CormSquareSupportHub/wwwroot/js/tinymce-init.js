tinymce.init({
    selector: '#templateEditor', // Selects the textarea
    plugins: 'image link lists advlist media table code',
    toolbar: 'undo redo | bold italic underline | alignleft aligncenter alignright | bullist numlist | link image media | code',
    height: 400,
    images_upload_url: '/Category/UploadImage', // Endpoint for image uploads
    automatic_uploads: true,
    file_picker_types: 'image',
    file_picker_callback: function (cb, value, meta) {
        var input = document.createElement('input');
        input.setAttribute('type', 'file');
        input.setAttribute('accept', 'image/*');
        input.onchange = function () {
            var file = this.files[0];
            var reader = new FileReader();
            reader.onload = function () {
                cb(reader.result, { title: file.name });
            };
            reader.readAsDataURL(file);
        };
        input.click();
    }
});
