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

document.querySelectorAll("[data-countdown]").forEach(element => {
    let remaining = Number(element.dataset.countdown || 0);
    const value = element.querySelector("[data-countdown-value]");
    const render = () => {
        const minutes = Math.floor(Math.max(0, remaining) / 60);
        const seconds = Math.max(0, remaining) % 60;
        if (value) value.textContent = `${minutes}:${seconds.toString().padStart(2, "0")}`;
        if (remaining > 0) remaining--;
    };
    render();
    window.setInterval(render, 1000);
});
const initializeAiCall = () => {
    const root = document.querySelector("[data-ai-call]");
    if (!root) return;

    const start = root.querySelector("[data-ai-call-start]");
    const stop = root.querySelector("[data-ai-call-stop]");
    const mute = root.querySelector("[data-ai-call-mute]");
    const video = root.querySelector(".ai-call-video");
    const placeholder = root.querySelector("[data-ai-call-placeholder]");
    const status = root.querySelector("[data-ai-call-status]");
    const duration = root.querySelector("[data-ai-call-duration]");
    const errorBox = root.querySelector("[data-ai-call-error]");
    const token = root.querySelector('input[name="__RequestVerificationToken"]')?.value;
    const audioForm = document.querySelector("[data-ai-audio-form]");
    const audioFile = audioForm?.querySelector("[data-ai-audio-file]");
    const conversation = document.querySelector("[data-ai-conversation]");
    const liveTranscript = conversation?.querySelector("[data-ai-live-transcript]");
    const provider = root.dataset.provider.toLowerCase();
    const connectionKey = `ai-call:${root.dataset.sessionId}`;
    const spokenKey = `${connectionKey}:spoken`;
    let client = null;
    let timer = null;
    let seconds = 0;
    let recorder = null;
    let audioStream = null;
    let audioChunks = [];

    const speak = (message) => {
        if (!message || !("speechSynthesis" in window)) return;
        window.speechSynthesis.cancel();
        const utterance = new SpeechSynthesisUtterance(message);
        utterance.lang = "fr-FR";
        utterance.rate = 1;
        window.speechSynthesis.speak(utterance);
    };

    const speakLatestOnce = () => {
        const messageId = root.dataset.latestMessageId;
        if (!messageId || sessionStorage.getItem(spokenKey) === messageId) return;
        sessionStorage.setItem(spokenKey, messageId);
        speak(root.dataset.latestMessage);
    };

    const renderLiveConversation = (messages) => {
        if (!liveTranscript || !Array.isArray(messages)) return;
        liveTranscript.replaceChildren();
        document.querySelector("[data-ai-empty]")?.classList.add("d-none");
        conversation.querySelectorAll("[data-ai-stored-message]")
            .forEach(message => message.classList.add("d-none"));

        messages
            .map(message => ({
                ...message,
                normalizedRole: message?.role === "persona" || message?.role === "assistant"
                    ? "assistant"
                    : message?.role === "user" ? "user" : null
            }))
            .filter(message => message.normalizedRole)
            .forEach(message => {
                const assistant = message.normalizedRole === "assistant";
                const article = document.createElement("article");
                article.className = `d-flex mb-3 ${assistant ? "" : "justify-content-end"}`;
                article.dataset.aiLiveMessage = "";

                const bubble = document.createElement("div");
                bubble.className = `p-3 rounded-3 ${assistant
                    ? "bg-white border" : "bg-primary text-white"}`;
                bubble.style.maxWidth = "85%";

                const author = document.createElement("div");
                author.className = "small fw-semibold mb-1";
                author.textContent = assistant ? root.dataset.trainerName : "Vous";

                const content = document.createElement("div");
                content.className = "ai-message-text";
                content.textContent = message.content ?? message.text ?? "";

                const source = document.createElement("small");
                source.className = assistant ? "text-secondary" : "text-white-50";
                source.textContent = assistant
                    ? "Conversation Anam en direct" : "Question vocale";

                bubble.append(author, content, source);
                article.append(bubble);
                liveTranscript.append(article);
            });

        conversation.scrollTop = conversation.scrollHeight;
    };

    const appendPersonaTranscript = (content) => {
        if (!liveTranscript || !content) return;
        document.querySelector("[data-ai-empty]")?.classList.add("d-none");
        conversation.querySelectorAll("[data-ai-stored-message]")
            .forEach(message => message.classList.add("d-none"));

        let article = liveTranscript.querySelector("[data-ai-streaming-persona]");
        if (!article) {
            article = document.createElement("article");
            article.className = "d-flex mb-3";
            article.dataset.aiStreamingPersona = "";

            const bubble = document.createElement("div");
            bubble.className = "p-3 rounded-3 bg-white border";
            bubble.style.maxWidth = "85%";

            const author = document.createElement("div");
            author.className = "small fw-semibold mb-1";
            author.textContent = root.dataset.trainerName;

            const text = document.createElement("div");
            text.className = "ai-message-text";
            text.dataset.aiStreamingText = "";

            const source = document.createElement("small");
            source.className = "text-secondary";
            source.textContent = "Réponse Anam en direct";

            bubble.append(author, text, source);
            article.append(bubble);
            liveTranscript.append(article);
        }
        article.querySelector("[data-ai-streaming-text]").textContent += content;
        conversation.scrollTop = conversation.scrollHeight;
    };

    const setConnected = (connected) => {
        start?.classList.toggle("d-none", connected);
        stop?.classList.toggle("d-none", !connected);
        mute?.classList.toggle("d-none", !connected);
        duration?.classList.toggle("d-none", !connected);
        root.classList.toggle("is-connected", connected);
    };

    const showError = (message) => {
        errorBox.textContent = message;
        errorBox.classList.remove("d-none");
        status.textContent = "Appel indisponible";
    };

    const beginTimer = () => {
        seconds = 0;
        clearInterval(timer);
        timer = setInterval(() => {
            seconds++;
            duration.textContent =
                `${String(Math.floor(seconds / 60)).padStart(2, "0")}:${String(seconds % 60).padStart(2, "0")}`;
        }, 1000);
    };

    const endCall = async () => {
        clearInterval(timer);
        if (client) {
            try { await client.stopStreaming(); } catch { /* Best-effort WebRTC cleanup. */ }
            client = null;
        }
        if (video) video.srcObject = null;
        placeholder?.classList.remove("d-none");
        status.textContent = "Appel terminé";
        sessionStorage.removeItem(connectionKey);
        window.speechSynthesis?.cancel();
        setConnected(false);
    };

    start?.addEventListener("click", async () => {
        start.disabled = true;
        errorBox.classList.add("d-none");
        status.textContent = "Connexion au formateur…";
        try {
            const response = await fetch(root.dataset.avatarEndpoint, {
                method: "POST",
                headers: { "RequestVerificationToken": token, "Accept": "application/json" }
            });
            const payload = await response.json();
            if (!response.ok) throw new Error(payload.error || "Impossible de démarrer l’appel.");

            if (payload.provider.toLowerCase() === "mock") {
                placeholder?.classList.remove("d-none");
                root.classList.add("is-simulated");
                status.textContent = "Appel de démonstration connecté";
                sessionStorage.setItem(connectionKey, "connected");
                if (mute) {
                    mute.setAttribute("aria-label", "Poser une question vocale");
                    mute.title = "Poser une question vocale";
                }
                speakLatestOnce();
            } else {
                if (!payload.clientToken) throw new Error("Le jeton vidéo reçu est invalide.");
                const { createClient, AnamEvent } = await import(
                    "https://cdn.jsdelivr.net/npm/@anam-ai/js-sdk@4.23.1/+esm"
                );
                client = createClient(payload.clientToken);
                client.addListener(AnamEvent.MESSAGE_HISTORY_UPDATED, messages => {
                    renderLiveConversation(messages);
                });
                client.addListener(AnamEvent.MESSAGE_STREAM_EVENT_RECEIVED, event => {
                    if (event?.role === "persona")
                        appendPersonaTranscript(event.content);
                });
                client.addListener(AnamEvent.USER_SPEECH_STARTED, () => {
                    status.textContent = "Je vous écoute…";
                });
                client.addListener(AnamEvent.USER_SPEECH_ENDED, () => {
                    status.textContent = `${root.dataset.trainerName} réfléchit…`;
                });
                client.addListener(AnamEvent.CONNECTION_ESTABLISHED, () => {
                    status.textContent = `En appel avec ${root.dataset.trainerName}`;
                });
                await client.streamToVideoElement(video.id);
                placeholder?.classList.add("d-none");
                status.textContent = `En appel avec ${root.dataset.trainerName}`;
            }
            setConnected(true);
            beginTimer();
        } catch (error) {
            showError(error instanceof Error ? error.message : "La connexion vidéo a échoué.");
        } finally {
            start.disabled = false;
        }
    });

    stop?.addEventListener("click", endCall);
    mute?.addEventListener("click", async () => {
        if (provider === "mock") {
            if (!audioForm || !audioFile || !navigator.mediaDevices?.getUserMedia ||
                !window.MediaRecorder) {
                showError("L’enregistrement vocal n’est pas pris en charge par ce navigateur.");
                return;
            }
            if (recorder?.state === "recording") {
                recorder.stop();
                return;
            }
            try {
                audioStream = await navigator.mediaDevices.getUserMedia({ audio: true });
                audioChunks = [];
                const mimeType = MediaRecorder.isTypeSupported("audio/webm")
                    ? "audio/webm" : "";
                recorder = new MediaRecorder(audioStream, mimeType ? { mimeType } : undefined);
                recorder.addEventListener("dataavailable", event => {
                    if (event.data.size > 0) audioChunks.push(event.data);
                });
                recorder.addEventListener("stop", () => {
                    audioStream?.getTracks().forEach(track => track.stop());
                    const type = recorder.mimeType || "audio/webm";
                    const blob = new Blob(audioChunks, { type });
                    const transfer = new DataTransfer();
                    transfer.items.add(new File([blob], "question.webm", { type }));
                    audioFile.files = transfer.files;
                    status.textContent = "Le formateur réfléchit…";
                    audioForm.submit();
                });
                recorder.start();
                mute.setAttribute("aria-pressed", "true");
                mute.setAttribute("aria-label", "Terminer la question vocale");
                mute.textContent = "⏹️";
                status.textContent = "Je vous écoute…";
            } catch {
                showError("Autorisez le microphone pour parler au formateur.");
            }
            return;
        }
        const muted = mute.getAttribute("aria-pressed") !== "true";
        mute.setAttribute("aria-pressed", String(muted));
        mute.textContent = muted ? "🔇" : "🎙️";
        if (client) {
            if (muted) client.muteInputAudio();
            else client.unmuteInputAudio();
        }
    });
    window.addEventListener("pagehide", () => { if (client) client.stopStreaming(); });

    if (provider === "mock" && sessionStorage.getItem(connectionKey) === "connected") {
        root.classList.add("is-simulated");
        status.textContent = "Appel de démonstration connecté";
        setConnected(true);
        if (mute) {
            mute.setAttribute("aria-label", "Poser une question vocale");
            mute.title = "Poser une question vocale";
        }
        beginTimer();
        speakLatestOnce();
    }
};

document.addEventListener("DOMContentLoaded", initializeAiCall);

document.getElementById("ai-message-form")?.addEventListener("submit", () => {
    document.getElementById("ai-loading")?.classList.remove("d-none");
});

const sessionTimer = document.getElementById("ai-time");
if (sessionTimer) {
    let remaining = Number(sessionTimer.dataset.seconds);
    setInterval(() => {
        if (remaining > 0) remaining--;
        const hours = String(Math.floor(remaining / 3600)).padStart(2, "0");
        const minutes = String(Math.floor(remaining % 3600 / 60)).padStart(2, "0");
        const seconds = String(remaining % 60).padStart(2, "0");
        sessionTimer.textContent = `${hours}:${minutes}:${seconds}`;
    }, 1000);
}
