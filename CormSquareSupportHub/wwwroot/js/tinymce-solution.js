tinymce.init({
    selector: '#editor',
    plugins: 'anchor autolink charmap codesample emoticons lists table visualblocks wordcount fullscreen image link media code',
    toolbar: 'undo redo | blocks fontfamily fontsize | bold italic underline strikethrough | align lineheight | numlist bullist indent outdent | codesample table link image media | code fullscreen',
    menubar: 'file edit insert view format table tools help',
    content_style: 'pre {background: #f4f4f4; padding: 10px; border-radius: 5px;}',
    images_upload_url: '/Admin/Solution/UploadImage',
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
    fullscreen_new_window: true,
    media_alt_source: false,
    media_poster: false,
    media_filter_html: true,
    setup: function (editor) {
        editor.on('init', function () {
            console.log('TinyMCE Solution editor initialized');
            $(document).trigger('tinymceInitialized');
        });
        editor.on('change', function () {
            document.getElementById('HtmlContent').value = editor.getContent();
        });
        editor.on('submit', function () {
            document.getElementById('HtmlContent').value = editor.getContent();
        });
    },
    file_picker_callback: function (cb, value, meta) {
        if (meta.filetype === 'image') {
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
    }
});