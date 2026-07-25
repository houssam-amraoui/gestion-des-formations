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

const contentType = document.querySelector("[data-content-type]");
const textFields = document.querySelector("[data-text-fields]");
const urlFields = document.querySelector("[data-url-fields]");
if (contentType && textFields && urlFields) {
    const updateContentFields = () => {
        const isText = contentType.value === "Text" || contentType.value === "1";
        textFields.hidden = !isText;
        urlFields.hidden = isText;
    };
    contentType.addEventListener("change", updateContentFields);
    updateContentFields();
}

document.querySelectorAll("[data-question-type]").forEach(select => {
    const form = select.closest("form");
    const expected = form?.querySelector("[data-expected-answer]");
    const help = form?.querySelector("[data-question-help]");
    const refresh = () => {
        const shortAnswer = select.value === "3";
        if (expected) expected.hidden = !shortAnswer;
        if (help) help.textContent = shortAnswer
            ? "Une réponse attendue est obligatoire. Aucun choix ne sera accepté."
            : "Ajoutez ensuite les choix de réponses avant de publier la question.";
    };
    select.addEventListener("change", refresh);
    refresh();
});
