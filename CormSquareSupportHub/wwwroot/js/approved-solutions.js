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

    // Chart
    const ctx = document.getElementById('categoryChart').getContext('2d');
    const chart = new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels: window.categoryCounts.labels,
            datasets: [{
                data: window.categoryCounts.data,
                backgroundColor: [
                    '#4e73df', '#1cc88a', '#36b9cc', '#f6c23e', '#e74a3b', '#858796', '#5a5c69'
                ]
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
});