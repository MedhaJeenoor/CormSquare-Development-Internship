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

//By default, TinyMCE doesn’t include a built -in code editor, but you can enable it using:

//Preformatted Text(<pre><code>...</code></pre>)
//Plugins like codesample(for syntax - highlighted code blocks)
//Custom Buttons(to insert / edit raw HTML)
//Example Configuration for TinyMCE with Code Support
//js
//Copy
//Edit
//tinymce.init({
//    selector: '#editor',  // Replace with your textarea ID
//    height: 400,
//    plugins: 'codesample table lists link image',
//    toolbar: 'undo redo | formatselect | bold italic underline | bullist numlist | codesample table',
//    content_style: 'pre {background: #f4f4f4; padding: 10px; border-radius: 5px;}'
//});
//This will allow users to:
//✅ Add and format text
//✅ Insert tables
//✅ Use lists, links, images
//✅ Include code blocks with syntax highlighting
