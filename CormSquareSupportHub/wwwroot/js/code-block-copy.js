document.addEventListener('DOMContentLoaded', () => {
    const contentContainers = document.querySelectorAll('.content-container');

    contentContainers.forEach(container => {
        const codeBlocks = container.querySelectorAll('pre code');

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
                e.stopPropagation(); // Prevent event bubbling to form
                const code = codeElement.textContent;
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
});