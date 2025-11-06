(() => {
    const root = document.querySelector('[data-pricing-root]');
    if (!root) {
        return;
    }

    const form = root.querySelector('[data-pricing-form]');
    const arrivalInput = form?.querySelector('[data-pricing-arrival]');
    const departureInput = form?.querySelector('[data-pricing-departure]');
    const guestsInput = form?.querySelector('[data-pricing-guests]');
    const emptyState = root.querySelector('[data-pricing-empty]');
    const loadingState = root.querySelector('[data-pricing-loading]');
    const errorState = root.querySelector('[data-pricing-error]');
    const summaryState = root.querySelector('[data-pricing-summary]');
    const nightsLabel = root.querySelector('[data-pricing-nights]');
    const breakdownList = root.querySelector('[data-pricing-breakdown]');
    const subtotalLabel = root.querySelector('[data-pricing-subtotal]');
    const taxRow = root.querySelector('[data-pricing-tax-row]');
    const taxLabel = root.querySelector('[data-pricing-tax]');
    const totalLabel = root.querySelector('[data-pricing-total]');

    if (!form || !arrivalInput || !departureInput || !guestsInput || !emptyState || !loadingState || !errorState || !summaryState || !nightsLabel || !breakdownList || !subtotalLabel || !taxRow || !taxLabel || !totalLabel) {
        return;
    }

    let inflightController = null;
    const currencyFormatters = new Map();

    const hide = (element) => element.classList.add('d-none');
    const show = (element) => element.classList.remove('d-none');

    const sanitizeGuests = () => {
        const min = Number.parseInt(guestsInput.min, 10) || 1;
        const max = Number.parseInt(guestsInput.max, 10) || undefined;
        let value = Number.parseInt(guestsInput.value, 10);
        if (Number.isNaN(value) || value < min) {
            value = min;
        }
        if (typeof max === 'number' && value > max) {
            value = max;
        }
        guestsInput.value = String(value);
        return value;
    };

    const adjustDepartureMin = () => {
        if (!arrivalInput.value) {
            return;
        }
        departureInput.min = arrivalInput.value;
        if (departureInput.value && departureInput.value <= arrivalInput.value) {
            const date = new Date(`${arrivalInput.value}T00:00:00`);
            date.setDate(date.getDate() + 1);
            departureInput.value = date.toISOString().slice(0, 10);
        }
    };

    const showEmpty = (message) => {
        if (message) {
            emptyState.textContent = message;
        }
        show(emptyState);
        hide(loadingState);
        hide(errorState);
        hide(summaryState);
    };

    const showLoading = () => {
        hide(emptyState);
        hide(errorState);
        hide(summaryState);
        show(loadingState);
    };

    const showError = (message) => {
        errorState.textContent = message;
        show(errorState);
        hide(emptyState);
        hide(loadingState);
        hide(summaryState);
    };

    const formatCurrency = (amount, currency) => {
        const safeCurrency = typeof currency === 'string' && currency.trim().length > 0 ? currency.trim() : 'DKK';
        const key = safeCurrency.toUpperCase();
        if (!currencyFormatters.has(key)) {
            try {
                currencyFormatters.set(key, new Intl.NumberFormat(undefined, { style: 'currency', currency: key }));
            }
            catch (err) {
                currencyFormatters.set(key, new Intl.NumberFormat(undefined, { style: 'currency', currency: 'DKK' }));
            }
        }
        const formatter = currencyFormatters.get(key);
        const numericAmount = typeof amount === 'number' ? amount : Number(amount ?? 0);
        return formatter.format(numericAmount);
    };

    const clearBreakdown = () => {
        breakdownList.innerHTML = '';
    };

    const diffInDays = (start, end) => {
        const startDate = new Date(`${start}T00:00:00`);
        const endDate = new Date(`${end}T00:00:00`);
        return Math.round((endDate - startDate) / (1000 * 60 * 60 * 24));
    };

    const renderSummary = (quote) => {
        const currency = quote?.currency ?? 'DKK';
        clearBreakdown();

        const items = Array.isArray(quote?.items) ? quote.items : [];
        items.forEach((item) => {
            const listItem = document.createElement('li');
            listItem.className = 'd-flex justify-content-between align-items-baseline gap-2';

            const label = document.createElement('span');
            label.textContent = item?.text ?? '';

            const amount = document.createElement('span');
            amount.className = 'fw-medium';
            amount.textContent = formatCurrency(item?.amount ?? 0, currency);

            listItem.appendChild(label);
            listItem.appendChild(amount);
            breakdownList.appendChild(listItem);
        });

        nightsLabel.textContent = String(quote?.nights ?? 0);
        subtotalLabel.textContent = formatCurrency(quote?.subtotal ?? 0, currency);

        const taxAmount = Number(quote?.tax ?? 0);
        if (taxAmount > 0) {
            taxLabel.textContent = formatCurrency(taxAmount, currency);
            show(taxRow);
        }
        else {
            taxLabel.textContent = formatCurrency(0, currency);
            hide(taxRow);
        }

        totalLabel.textContent = formatCurrency(quote?.total ?? 0, currency);
        hide(loadingState);
        hide(emptyState);
        hide(errorState);
        show(summaryState);
    };

    const requestQuote = async () => {
        const arrival = arrivalInput.value;
        const departure = departureInput.value;
        if (!arrival || !departure) {
            showEmpty('Vælg ankomst og afrejse for at se prisen.');
            return;
        }

        const nights = diffInDays(arrival, departure);
        if (!Number.isFinite(nights) || nights <= 0) {
            showError('Afrejse skal ligge efter ankomst.');
            return;
        }

        const guests = sanitizeGuests();
        const houseId = form.dataset.houseId;
        if (!houseId) {
            showError('Manglende hus-id.');
            return;
        }

        const payload = {
            houseId,
            arrival,
            departure,
            guests,
            areaId: form.dataset.areaId && form.dataset.areaId.length > 0 ? form.dataset.areaId : null
        };

        inflightController?.abort();
        const controller = new AbortController();
        inflightController = controller;

        showLoading();

        try {
            const response = await fetch(form.dataset.quoteUrl, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Accept': 'application/json'
                },
                body: JSON.stringify(payload),
                signal: controller.signal
            });

            const contentType = response.headers.get('content-type') ?? '';
            const isJson = contentType.includes('application/json');
            const data = isJson ? await response.json() : null;

            if (response.ok) {
                renderSummary(data ?? {});
                return;
            }

            if (response.status === 400 && data?.errors) {
                const errorMessages = Object.values(data.errors).flat().filter(Boolean);
                showError(errorMessages[0] ?? 'Ugyldig forespørgsel.');
                return;
            }

            if (data?.message) {
                showError(data.message);
                return;
            }

            showError('Kunne ikke hente pris. Prøv igen.');
        }
        catch (err) {
            if (err.name === 'AbortError') {
                return;
            }
            showError('Netværksfejl. Prøv igen.');
        }
    };

    const scheduleQuote = () => {
        requestQuote();
    };

    form.addEventListener('submit', (event) => {
        event.preventDefault();
        scheduleQuote();
    });

    arrivalInput.addEventListener('change', () => {
        adjustDepartureMin();
        scheduleQuote();
    });

    departureInput.addEventListener('change', scheduleQuote);
    guestsInput.addEventListener('change', () => {
        sanitizeGuests();
        scheduleQuote();
    });

    adjustDepartureMin();
    sanitizeGuests();

    if (arrivalInput.value && departureInput.value) {
        scheduleQuote();
    }
    else {
        showEmpty('Vælg ankomst og afrejse for at se prisen.');
    }
})();
