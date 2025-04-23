console.log("Script starting: tinymce-init.js (v1.1)");

try {
    tinymce.init({
        selector: '#editor',
        plugins: 'anchor autolink charmap codesample emoticons lists table visualblocks wordcount fullscreen image link media code',
        toolbar: 'undo redo | blocks fontfamily fontsize | bold italic underline strikethrough | alignleft aligncenter alignright alignjustify | numlist bullist indent outdent | codesample table link image media | code fullscreen',
        menubar: 'file edit insert view format table tools help',
        content_style: 'pre { background: #f4f4f4; padding: 10px; border-radius: 5px; }',
        images_upload_url: '/Admin/Category/UploadImage',
        automatic_uploads: true,
        file_picker_types: 'image',
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
        fullscreen_native: true,
        media_alt_source: false,
        media_poster: false,
        media_filter_html: true,
        setup: function (editor) {
            editor.on('init', function () {
                console.log('TinyMCE Category editor initialized');
                jQuery(document).trigger('tinymceInitialized');
            });
            editor.on('change', function () {
                const content = editor.getContent();
                document.getElementById('HtmlContent').value = content;
                console.log('Editor content updated on change:', content);
            });
        },
        file_picker_callback: function (callback, value, meta) {
            if (meta.filetype === 'image') {
                var input = document.createElement('input');
                input.setAttribute('type', 'file');
                input.setAttribute('accept', 'image/*');
                input.onchange = function () {
                    var file = this.files[0];
                    var reader = new FileReader();
                    reader.onload = function () {
                        callback(reader.result, { alt: file.name });
                        console.log('Image selected for TinyMCE:', file.name);
                    };
                    reader.readAsDataURL(file);
                };
                input.click();
            }
        }
    });
} catch (e) {
    console.error("TinyMCE initialization failed:", e);
    toastr.error("Failed to load TinyMCE editor. Please check your internet connection or refresh the page.");
}