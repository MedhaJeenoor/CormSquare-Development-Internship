document.addEventListener('DOMContentLoaded', () => {
    // Client-Product mapping
    const clientProducts = window.clientProducts;

    // Filtering Logic
    const searchInput = document.getElementById("searchInput");
    const clientFilter = document.getElementById("clientFilter");
    const productFilter = document.getElementById("productFilter");
    const clearBtn = document.getElementById("clearFilters");
    const rows = document.querySelectorAll("#solutionsTable tbody tr");

    function updateProductFilter(client) {
        productFilter.innerHTML = '<option value="">Filter by Product</option>';
        if (client && clientProducts && clientProducts[client]) {
            clientProducts[client].forEach(product => {
                const option = document.createElement("option");
                option.value = product;
                option.textContent = product;
                productFilter.appendChild(option);
            });
        }
    }

    clientFilter.addEventListener("change", () => {
        updateProductFilter(clientFilter.value);
        filterSolutions();
    });

    function filterSolutions() {
        const search = searchInput.value.toLowerCase();
        const client = clientFilter.value.toLowerCase();
        const product = productFilter.value.toLowerCase();

        rows.forEach(row => {
            const title = row.dataset.title || "";
            const category = row.dataset.category || "";
            const rowClient = row.dataset.client || "";
            const rowProduct = row.dataset.product || "";
            const author = row.dataset.author || "";
            const content = row.dataset.content || "";
            const issue = row.dataset.issue || "";
            const docid = row.dataset.docid || "";

            const matchesSearch =
                title.includes(search) ||
                category.includes(search) ||
                rowClient.includes(search) ||
                rowProduct.includes(search) ||
                author.includes(search) ||
                content.includes(search) ||
                issue.includes(search) ||
                docid.toLowerCase().includes(search);

            const matchesClient = client === "" || rowClient.includes(client);
            const matchesProduct = product === "" || rowProduct.includes(product);

            row.style.display = matchesSearch && matchesClient && matchesProduct ? "" : "none";
        });
    }

    searchInput.addEventListener("input", filterSolutions);
    clientFilter.addEventListener("change", filterSolutions);
    productFilter.addEventListener("change", filterSolutions);
    clearBtn.addEventListener("click", () => {
        searchInput.value = "";
        clientFilter.value = "";
        productFilter.value = "";
        updateProductFilter("");
        filterSolutions();
    });

    // Function to generate dynamic colors based on data length
    function generateColors(count) {
        const colors = [];
        for (let i = 0; i < count; i++) {
            const hue = (i * 360 / count) % 360;
            colors.push(`hsl(${hue}, 70%, 60%)`);
        }
        return colors;
    }

    // Chart
    const ctx = document.getElementById('categoryChart').getContext('2d');
    const chart = new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels: window.categoryCounts.labels,
            datasets: [{
                data: window.categoryCounts.data,
                backgroundColor: generateColors(window.categoryCounts.data.length)
            }]
        },
        options: {
            responsive: true,
            plugins: {
                legend: {
                    display: false
                },
                tooltip: {
                    enabled: true,
                    callbacks: {
                        title: function () {
                            return '';
                        },
                        label: function (context) {
                            let label = context.label || 'Uncategorized';
                            let value = context.raw || 0;
                            return `${label}: ${value} solutions`;
                        }
                    }
                }
            },
            cutout: '60%',
            layout: {
                padding: 20
            }
        }
    });

    // Generate scrollable legend
    const legendContainer = document.getElementById('chartLegend');
    const labels = window.categoryCounts.labels;
    const data = window.categoryCounts.data;
    const colors = generateColors(data.length);
    const total = data.reduce((a, b) => a + b, 0);

    labels.forEach((label, index) => {
        const percent = ((data[index] / total) * 100).toFixed(1);
        const legendItem = document.createElement('div');
        legendItem.style.display = 'flex';
        legendItem.style.alignItems = 'center';
        legendItem.style.marginBottom = '8px';
        legendItem.innerHTML = `
        <span style="display:inline-block; width:12px; height:12px; background-color:${colors[index]}; margin-right:6px; border-radius:2px;"></span>
        ${label}: ${data[index]} (${percent}%)
    `;
        legendContainer.appendChild(legendItem);
    });
});