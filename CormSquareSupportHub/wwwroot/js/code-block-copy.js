document.addEventListener('DOMContentLoaded', () => {
    console.log("Script starting: code-block-copy.js (v1.1)");

    try {
        const contentContainers = document.querySelectorAll('.content-container');
        console.log('Found content containers:', contentContainers.length);

        contentContainers.forEach(container => {
            const codeBlocks = container.querySelectorAll('pre code');
            console.log('Found code blocks in container:', codeBlocks.length);

            codeBlocks.forEach(codeElement => {
                const preElement = codeElement.parentElement;
                const wrapper = document.createElement('div');
                wrapper.className = 'code-block-container';

                // Create copy button with type="button"
                const copyButton = document.createElement('button');
                copyButton.type = 'button'; // Explicitly set type to prevent form submission
                copyButton.className = 'copy-button';
                copyButton.textContent = 'Copy';

                // Insert wrapper and button
                preElement.parentNode.insertBefore(wrapper, preElement);
                wrapper.appendChild(copyButton);
                wrapper.appendChild(preElement);

                // Add click event for copying
                copyButton.addEventListener('click', (e) => {
                    e.preventDefault(); // Prevent any default behavior
                    e.stopPropagation(); // Prevent event bubbling to form
                    const code = codeElement.textContent;
                    console.log('Copy button clicked, copying code:', code.substring(0, 50) + '...');
                    navigator.clipboard.writeText(code).then(() => {
                        copyButton.textContent = 'Copied!';
                        console.log('Code copied successfully');
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
    } catch (err) {
        console.error('Error in code-block-copy.js:', err);
    }
});