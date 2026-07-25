document.querySelectorAll("form[data-confirm]").forEach(form => {
    form.addEventListener("submit", event => {
        if (!window.confirm(form.dataset.confirm)) event.preventDefault();
    });
});

const slugSource = document.querySelector("[data-slug-source]");
const slugTarget = document.querySelector("[data-slug-target]");
if (slugSource && slugTarget) {
    let manuallyEdited = slugTarget.value.length > 0;
    slugTarget.addEventListener("input", () => { manuallyEdited = slugTarget.value.length > 0; });
    slugSource.addEventListener("input", () => {
        if (manuallyEdited) return;
        slugTarget.value = slugSource.value.normalize("NFD").replace(/[\u0300-\u036f]/g, "")
            .toLowerCase().replace(/[^a-z0-9]+/g, "-").replace(/^-|-$/g, "");
    });
}

const freeToggle = document.querySelector("[data-free-toggle]");
const priceInput = document.querySelector("[data-price]");
if (freeToggle && priceInput) {
    const updatePrice = () => {
        priceInput.disabled = freeToggle.checked;
        if (freeToggle.checked) priceInput.value = "0";
    };
    freeToggle.addEventListener("change", updatePrice);
    updatePrice();
}
