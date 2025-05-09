console.log("Script starting: tinymce-solution.js (v1.2)");

// Check if Prism.js is loaded
if (typeof Prism === 'undefined') {
    console.error("Prism.js is not loaded. The codesample plugin requires Prism.js for syntax highlighting.");
} else {
    console.log("Prism.js is loaded successfully.");
}

try {
    tinymce.init({
        selector: '#editor',
        plugins: 'anchor autolink charmap codesample emoticons lists table visualblocks wordcount fullscreen image link media code',
        toolbar: 'undo redo | blocks fontfamily fontsize | bold italic underline strikethrough | align lineheight | numlist bullist indent outdent | codesample table link image media | copycode | code fullscreen',
        menubar: 'file edit insert view format table tools help',
        content_style: 'pre { background: #f4f4f4; padding: 10px; border-radius: 5px; white-space: pre-wrap; display: block; visibility: visible; } .mce-copy-button { position: absolute; top: -30px; right: 0; background: #333; color: white; border: none; padding: 5px 10px; cursor: pointer; }',
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
        codesample_global_prismjs: true,
        fullscreen_native: true,
        fullscreen_new_window: true,
        media_alt_source: false,
        media_poster: false,
        media_filter_html: true,
        setup: function (editor) {
            // Add a "Copy Code" button in the toolbar
            editor.ui.registry.addButton('copycode', {
                text: 'Copy Code',
                tooltip: 'Copy selected code block',
                onAction: function () {
                    const selectedNode = editor.selection.getNode();
                    const codeBlock = selectedNode.closest('pre');
                    if (codeBlock) {
                        const code = codeBlock.textContent;
                        navigator.clipboard.writeText(code).then(() => {
                            editor.notificationManager.open({
                                text: 'Code copied to clipboard!',
                                type: 'success'
                            });
                        }).catch(err => {
                            editor.notificationManager.open({
                                text: 'Failed to copy code.',
                                type: 'error'
                            });
                        });
                    } else {
                        editor.notificationManager.open({
                            text: 'Please select a code block.',
                            type: 'info'
                        });
                    }
                }
            });

            editor.on('init', function () {
                console.log('TinyMCE Solution editor initialized');
                // Check if the codesample plugin is loaded
                if (editor.plugins.codesample) {
                    console.log('Codesample plugin is loaded successfully.');
                } else {
                    console.error('Codesample plugin failed to load.');
                }
                jQuery(document).trigger('tinymceInitialized');
            });

            editor.on('change', function () {
                var content = editor.getContent();
                document.getElementById('HtmlContent').value = content;
                console.log('Editor content updated on change:', content);
            });

            editor.on('submit', function () {
                document.getElementById('HtmlContent').value = editor.getContent();
            });

            // Add a "Copy" button above each <pre> tag in the editor
            editor.on('NodeChange', function (e) {
                const pres = editor.getBody().querySelectorAll('pre');
                pres.forEach(pre => {
                    // Skip if the <pre> tag is being inserted by codesample (has language-xxx class)
                    if (pre.className.includes('language-') && !pre.getAttribute('data-copy-button-added')) {
                        console.log('Skipping <pre> tag with language class during NodeChange:', pre.outerHTML);
                        return;
                    }

                    // Check if the <pre> tag already has a copy button
                    let copyButton = pre.previousSibling;
                    if (copyButton && copyButton.className === 'mce-copy-button') {
                        console.log('Copy button already exists for <pre> tag:', pre.outerHTML);
                        return;
                    }

                    console.log('Adding copy button to <pre> tag:', pre.outerHTML);

                    const wrapper = editor.dom.create('div', { style: 'position: relative;' });
                    pre.parentNode.insertBefore(wrapper, pre);
                    wrapper.appendChild(pre);

                    copyButton = editor.dom.create('button', {
                        class: 'mce-copy-button',
                        text: 'Copy'
                    });

                    wrapper.insertBefore(copyButton, pre);

                    // Mark the <pre> tag to avoid reprocessing
                    pre.setAttribute('data-copy-button-added', 'true');

                    copyButton.addEventListener('click', function () {
                        const code = pre.textContent;
                        navigator.clipboard.writeText(code).then(() => {
                            copyButton.textContent = 'Copied!';
                            setTimeout(() => {
                                copyButton.textContent = 'Copy';
                            }, 2000);
                        }).catch(err => {
                            console.error('Failed to copy code:', err);
                            copyButton.textContent = 'Error';
                            setTimeout(() => {
                                copyButton.textContent = 'Copy';
                            }, 2000);
                        });
                    });
                });
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
} catch (e) {
    console.error("TinyMCE initialization failed:", e);
    toastr.error("Failed to load TinyMCE editor. Please check your internet connection or refresh the page.");
}