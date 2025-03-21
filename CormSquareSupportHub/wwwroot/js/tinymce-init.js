//tinymce.init({
//    selector: '#editor',
//    plugins: 'anchor autolink charmap codesample emoticons lists table visualblocks wordcount fullscreen image link media code',
//    toolbar: 'undo redo | blocks fontfamily fontsize | bold italic underline strikethrough | align lineheight | numlist bullist indent outdent | codesample table link image media | code fullscreen',
//    content_style: 'pre {background: #f4f4f4; padding: 10px; border-radius: 5px;}',
//    setup: function (editor) {
//        editor.on('change', function () {
//            document.getElementById("HtmlContent").value = editor.getContent();
//        });
//    }
//});

tinymce.init({
    selector: '#editor', // Selects the textarea
    //height: 400,
    plugins: 'anchor autolink charmap codesample emoticons lists table visualblocks wordcount fullscreen image link media code',
    toolbar: 'undo redo | blocks fontfamily fontsize | bold italic underline strikethrough | align lineheight | numlist bullist indent outdent | codesample table link image media | code fullscreen',
    menubar: 'file edit insert view format table tools help',
    content_style: 'pre {background: #f4f4f4; padding: 10px; border-radius: 5px;}',

    // Auto-save content to hidden input on change
    setup: function (editor) {
        // When TinyMCE content changes, update the hidden input
        editor.on('change', function () {
            document.getElementById('HtmlContent').value = editor.getContent();
        });

        // Ensure latest content is saved on form submission
        document.querySelector('form').addEventListener('submit', function () {
            document.getElementById('HtmlContent').value = editor.getContent();
        });
    },

    // Enable automatic uploads for images
    images_upload_url: '/Category/UploadImage', // Endpoint for image uploads
    automatic_uploads: true,
    file_picker_types: 'image',

    // Custom File Picker for Images
    file_picker_callback: function (cb, value, meta) {
        if (meta.filetype === 'image') {
            var input = document.createElement('input');
            input.setAttribute('type', 'file');
            input.setAttribute('accept', 'image/*');

            input.onchange = function () {
                var file = this.files[0];
                var reader = new FileReader();

                reader.onload = function () {
                    // Pass the base64 data to TinyMCE
                    cb(reader.result, { title: file.name });
                };

                reader.readAsDataURL(file);
            };

            input.click();
        }
    },

    // Handle code blocks with syntax highlighting
    codesample_languages: [
        { text: 'HTML/XML', value: 'markup' },
        { text: 'JavaScript', value: 'javascript' },
        { text: 'CSS', value: 'css' },
        { text: 'PHP', value: 'php' },
        { text: 'Python', value: 'python' },
        { text: 'Ruby', value: 'ruby' },
        { text: 'Java', value: 'java' },
        { text: 'C', value: 'c' },
        { text: 'C#', value: 'csharp' },
        { text: 'SQL', value: 'sql' }
    ],

    // Fullscreen mode settings
    fullscreen_native: true,
    fullscreen_new_window: true,

    // Enable advanced media embedding
    media_alt_source: false,
    media_poster: false,
    media_filter_html: true
});


    //setup: function (editor) {
    //    // When TinyMCE content changes, update the hidden input
    //    editor.on('change', function () {
    //        document.getElementById('TemplateJson').value = editor.getContent();
    //    });

    //    // When form is submitted, ensure latest content is saved
    //    document.getElementById('categoryForm').addEventListener('submit', function () {
    //        document.getElementById('TemplateJson').value = editor.getContent();
    //    });
    //}
//});



//tinymce.init({
//    selector: '#templateEditor', // Selects the textarea
//    plugins: 'image link lists advlist media table code',
//    toolbar: 'undo redo | bold italic underline | alignleft aligncenter alignright | bullist numlist | link image media | code',
//    height: 400,
//    images_upload_url: '/Category/UploadImage', // Endpoint for image uploads
//    automatic_uploads: true,
//    file_picker_types: 'image',
//    file_picker_callback: function (cb, value, meta) {
//        var input = document.createElement('input');
//        input.setAttribute('type', 'file');
//        input.setAttribute('accept', 'image/*');
//        input.onchange = function () {
//            var file = this.files[0];
//            var reader = new FileReader();
//            reader.onload = function () {
//                cb(reader.result, { title: file.name });
//            };
//            reader.readAsDataURL(file);
//        };
//        input.click();
//    }
//});

////By default, TinyMCE doesn’t include a built -in code editor, but you can enable it using:

////Preformatted Text(<pre><code>...</code></pre>)
////Plugins like codesample(for syntax - highlighted code blocks)
////Custom Buttons(to insert / edit raw HTML)
////Example Configuration for TinyMCE with Code Support
////js
////Copy
////Edit
////tinymce.init({
////    selector: '#editor',  // Replace with your textarea ID
////    height: 400,
////    plugins: 'codesample table lists link image',
////    toolbar: 'undo redo | formatselect | bold italic underline | bullist numlist | codesample table',
////    content_style: 'pre {background: #f4f4f4; padding: 10px; border-radius: 5px;}'
////});
////This will allow users to:
////✅ Add and format text
////✅ Insert tables
////✅ Use lists, links, images
////✅ Include code blocks with syntax highlighting
