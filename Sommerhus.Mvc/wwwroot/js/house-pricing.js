// Price and availability widget for the house page and the booking form.
//
// A [data-pricing-root] element holds a [data-pricing-form] with the arrival, departure and guest
// inputs, and the states and texts rendered by Views/Houses/_PricingWidget.cshtml. By default the
// widget owns the form and quotes on submit. With data-pricing-mode="booking" the form submits as
// usual once a price is shown: its [data-pricing-submit] button is disabled while there is no price
// or an error, and a submit made while a quote loads is sent when that quote succeeds. The server
// validates the booking regardless. Anchors marked [data-pricing-link] anywhere on the page follow
// the current selection.
(() => {
    'use strict';

    // English fallbacks, used only when the page carries no texts.
    const DEFAULT_TEXTS = {
        selectDates: 'Choose arrival and departure to see the price.',
        departureAfterArrival: 'Departure must be after arrival.',
        quoteFailed: 'The price could not be calculated right now. Please try again later.',
        networkError: 'Network error. Please try again.',
        nightOne: '{n} night',
        nightMany: '{n} nights',
        lineBase: '{nights} · {season}',
        lineGuests: 'Extra guests: {guests} × {nights}',
        lineCleaning: 'Final cleaning',
        available: 'Available',
        unavailable: 'Not available',
        unknown: 'Unknown',
        waiting: 'Waiting for dates',
        invalidDates: 'Choose valid dates to check availability.',
        checking: 'Checking availability...',
        loadError: 'Could not load availability right now.',
        availableMessage: 'The selected dates look available.',
        unavailableMessage: 'The selected dates are not available',
        blockingSingular: 'blocking',
        blockingPlural: 'blockings'
    };

    const pad = (value) => String(value).padStart(2, '0');

    // Date inputs hold local calendar dates. toISOString() converts to UTC first, which east of
    // Greenwich turns local midnight into the previous day.
    const formatDate = (date) => `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;

    // While a date is typed on the keyboard the browser reports each partial year as a date
    // (0002-07-03, 0020-07-03, ...). Years before 1000 are treated as not yet a date.
    const parseDate = (value) => {
        const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value ?? '');
        if (!match || Number(match[1]) < 1000) {
            return null;
        }
        return new Date(Number(match[1]), Number(match[2]) - 1, Number(match[3]));
    };

    // The longest stay the widget keeps when arrival moves past departure; the API allows 365 nights.
    const MAX_STAY_NIGHTS = 365;

    const addDays = (value, days) => {
        const date = parseDate(value);
        if (!date) {
            return value;
        }
        date.setDate(date.getDate() + days);
        return formatDate(date);
    };

    // Whole days between two dates, unaffected by daylight saving time.
    const daysBetween = (start, end) => {
        const from = parseDate(start);
        const to = parseDate(end);
        if (!from || !to) {
            return Number.NaN;
        }
        const utc = (date) => Date.UTC(date.getFullYear(), date.getMonth(), date.getDate());
        return Math.round((utc(to) - utc(from)) / 86400000);
    };

    const fill = (template, values) =>
        String(template ?? '').replace(/\{(\w+)\}/g, (token, name) => (name in values ? String(values[name]) : token));

    const readTexts = (root) => {
        const node = root.querySelector('script[data-pricing-texts]');
        if (!node) {
            return { ...DEFAULT_TEXTS };
        }
        try {
            return { ...DEFAULT_TEXTS, ...JSON.parse(node.textContent || '{}') };
        }
        catch {
            return { ...DEFAULT_TEXTS };
        }
    };

    // Amounts follow the page's culture, not the browser's.
    const currencyFormatters = new Map();
    const formatCurrency = (amount, currency) => {
        const code = (typeof currency === 'string' && currency.trim().length > 0 ? currency.trim() : 'DKK').toUpperCase();
        if (!currencyFormatters.has(code)) {
            const locale = document.documentElement.lang || undefined;
            let formatter;
            try {
                formatter = new Intl.NumberFormat(locale, { style: 'currency', currency: code });
            }
            catch {
                try {
                    formatter = new Intl.NumberFormat(locale, { style: 'currency', currency: 'DKK' });
                }
                catch {
                    formatter = new Intl.NumberFormat(undefined, { style: 'currency', currency: 'DKK' });
                }
            }
            currencyFormatters.set(code, formatter);
        }
        const value = typeof amount === 'number' ? amount : Number(amount ?? 0);
        return currencyFormatters.get(code).format(Number.isFinite(value) ? value : 0);
    };

    const setVisible = (element, visible) => element?.classList.toggle('d-none', !visible);

    const initWidget = (root) => {
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
        const subtotalRow = root.querySelector('[data-pricing-subtotal-row]');
        const subtotalLabel = root.querySelector('[data-pricing-subtotal]');
        const taxRow = root.querySelector('[data-pricing-tax-row]');
        const taxLabel = root.querySelector('[data-pricing-tax]');
        const totalLabel = root.querySelector('[data-pricing-total]');
        const vatRow = root.querySelector('[data-pricing-vat-row]');
        const vatLabel = root.querySelector('[data-pricing-vat]');
        const vatNote = root.querySelector('[data-pricing-vat-note]');

        const required = [form, arrivalInput, departureInput, guestsInput, emptyState, loadingState, errorState,
            summaryState, nightsLabel, breakdownList, subtotalLabel, taxLabel, totalLabel];
        if (required.some((element) => !element)) {
            return;
        }

        const texts = readTexts(root);
        const bookingMode = root.dataset.pricingMode === 'booking';
        const submitButton = root.querySelector('[data-pricing-submit]');
        const availability = root.querySelector('[data-availability-widget]');

        let state = 'empty';
        let submitting = false;
        let submitPending = false;
        let hasSummary = false;
        let quoteController = null;
        let availabilityController = null;
        let stayNights = 7;

        // The button stays enabled while a quote loads. Changing a field and then clicking the
        // button blurs the field, which starts a new quote between mousedown and mouseup; a button
        // disabled at that moment would swallow the click. A submit during loading waits for the
        // quote instead (see the submit handler).
        const updateSubmit = () => {
            if (submitButton) {
                submitButton.disabled = submitting || (state !== 'ready' && state !== 'loading');
                submitButton.toggleAttribute('aria-busy', submitPending);
            }
        };

        const setState = (next, message) => {
            state = next;
            if (next === 'empty' && message) {
                emptyState.textContent = message;
            }
            if (next === 'error') {
                errorState.textContent = message ?? texts.quoteFailed;
            }
            if (next === 'ready') {
                hasSummary = true;
            }
            else if (next !== 'loading') {
                hasSummary = false;
            }

            // A price already on screen stays in place, dimmed, while the next one loads, so nothing
            // below it moves under the pointer. The spinner row is shown only when there is none.
            const keepSummary = next === 'loading' && hasSummary;
            setVisible(emptyState, next === 'empty');
            setVisible(loadingState, next === 'loading' && !keepSummary);
            setVisible(errorState, next === 'error');
            setVisible(summaryState, next === 'ready' || keepSummary);
            summaryState.classList.toggle('opacity-50', keepSummary);
            summaryState.toggleAttribute('aria-busy', next === 'loading');

            if (next !== 'loading' && submitPending) {
                submitPending = false;
                if (next === 'ready') {
                    // The quote the visitor was waiting for is in; send the form they submitted.
                    if (typeof form.requestSubmit === 'function') {
                        form.requestSubmit(submitButton ?? undefined);
                    }
                    else {
                        submitting = true;
                        form.submit();
                    }
                }
            }
            updateSubmit();
        };

        const sanitizeGuests = () => {
            const min = Number.parseInt(guestsInput.min, 10) || 1;
            const max = Number.parseInt(guestsInput.max, 10);
            let value = Number.parseInt(guestsInput.value, 10);
            if (Number.isNaN(value) || value < min) {
                value = min;
            }
            if (!Number.isNaN(max) && value > max) {
                value = max;
            }
            guestsInput.value = String(value);
            return value;
        };

        // A date the visitor could mean: complete, and not before the input's minimum.
        const usableDate = (input) => parseDate(input.value) !== null && (!input.min || input.value >= input.min);

        // Departure must stay after arrival. When moving arrival passes it, the stay keeps its length.
        const adjustDeparture = () => {
            if (!usableDate(arrivalInput)) {
                return;
            }
            departureInput.min = addDays(arrivalInput.value, 1);
            if (!parseDate(departureInput.value) || departureInput.value <= arrivalInput.value) {
                departureInput.value = addDays(arrivalInput.value, stayNights);
            }
        };

        // Only a stay the visitor chose counts: set from the departure field and at start, never from
        // an arrival change, where the new arrival and the old departure are not a length they picked.
        const rememberStay = () => {
            const nights = daysBetween(arrivalInput.value, departureInput.value);
            if (Number.isFinite(nights) && nights > 0) {
                stayNights = Math.min(nights, MAX_STAY_NIGHTS);
            }
        };

        const updateLinks = () => {
            const params = new URLSearchParams();
            if (arrivalInput.value) {
                params.set('checkIn', arrivalInput.value);
            }
            if (departureInput.value) {
                params.set('checkOut', departureInput.value);
            }
            params.set('guests', guestsInput.value);

            document.querySelectorAll('[data-pricing-link]').forEach((link) => {
                const target = `${link.dataset.pricingLink}?${params}`;
                const login = link.dataset.pricingLogin;
                link.href = login ? `${login}?returnUrl=${encodeURIComponent(target)}` : target;
            });
        };

        const nightsText = (count) => fill(count === 1 ? texts.nightOne : texts.nightMany, { n: count });

        // The API sends structured fields; a quote cached before it did has only its English text.
        const lineText = (item) => {
            const code = String(item?.code ?? '').toUpperCase();
            const nights = Number.isInteger(item?.nights) ? item.nights : null;
            if (code === 'BASE' && nights !== null) {
                const season = item.seasonName || item.seasonCode;
                return season ? fill(texts.lineBase, { nights: nightsText(nights), season }) : nightsText(nights);
            }
            if (code === 'GUEST' && nights !== null && Number.isInteger(item?.guests)) {
                return fill(texts.lineGuests, { guests: item.guests, nights: nightsText(nights) });
            }
            if (code === 'CLEAN') {
                return texts.lineCleaning;
            }
            return item?.text ?? '';
        };

        const renderSummary = (quote) => {
            const currency = quote?.currency ?? 'DKK';
            const items = Array.isArray(quote?.items) ? quote.items : [];
            breakdownList.replaceChildren(...items.map((item) => {
                const listItem = document.createElement('li');
                listItem.className = 'd-flex justify-content-between align-items-baseline gap-2';

                const label = document.createElement('span');
                label.textContent = lineText(item);

                const amount = document.createElement('span');
                amount.className = 'fw-medium text-nowrap';
                amount.textContent = formatCurrency(item?.amount ?? 0, currency);

                listItem.append(label, amount);
                return listItem;
            }));

            nightsLabel.textContent = String(quote?.nights ?? 0);

            // VAT added on top (older API versions) needs the subtotal beside it. VAT included in
            // the total is shown as its share instead.
            const tax = Number(quote?.tax ?? 0);
            subtotalLabel.textContent = formatCurrency(quote?.subtotal ?? 0, currency);
            taxLabel.textContent = formatCurrency(tax, currency);
            setVisible(subtotalRow, tax > 0);
            setVisible(taxRow, tax > 0);

            totalLabel.textContent = formatCurrency(quote?.total ?? 0, currency);

            const vatIncluded = Number(quote?.vatIncluded ?? 0);
            if (vatLabel) {
                vatLabel.textContent = formatCurrency(vatIncluded, currency);
            }
            setVisible(vatRow, vatIncluded > 0);
            setVisible(vatNote, vatIncluded > 0);

            setState('ready');
        };

        const requestQuote = async () => {
            const arrival = arrivalInput.value;
            const departure = departureInput.value;
            if (!usableDate(arrivalInput) || !parseDate(departure)) {
                quoteController?.abort();
                setState('empty', texts.selectDates);
                return;
            }

            const nights = daysBetween(arrival, departure);
            if (!Number.isFinite(nights) || nights <= 0) {
                quoteController?.abort();
                setState('error', texts.departureAfterArrival);
                return;
            }

            const payload = {
                houseId: form.dataset.houseId,
                arrival,
                departure,
                guests: sanitizeGuests(),
                areaId: form.dataset.areaId ? form.dataset.areaId : null
            };

            quoteController?.abort();
            const controller = new AbortController();
            quoteController = controller;
            setState('loading');

            try {
                const response = await fetch(form.dataset.quoteUrl, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json', 'Accept': 'application/json' },
                    body: JSON.stringify(payload),
                    signal: controller.signal
                });

                const isJson = (response.headers.get('content-type') ?? '').includes('application/json');
                const data = isJson ? await response.json() : null;
                if (controller !== quoteController) {
                    return;
                }

                if (response.ok) {
                    renderSummary(data ?? {});
                    return;
                }

                // The MVC proxy answers every failure with a localized message.
                setState('error', typeof data?.message === 'string' && data.message ? data.message : texts.quoteFailed);
            }
            catch (err) {
                if (err?.name === 'AbortError' || controller !== quoteController) {
                    return;
                }
                setState('error', texts.networkError);
            }
        };

        const statusEl = availability?.querySelector('[data-availability-status]');
        const messageEl = availability?.querySelector('[data-availability-message]');

        const setAvailability = (kind, message) => {
            if (!statusEl || !messageEl) {
                return;
            }
            const variants = {
                ok: ['text-bg-success', texts.available],
                blocked: ['text-bg-danger', texts.unavailable],
                error: ['text-bg-warning', texts.unknown]
            };
            const [className, label] = variants[kind] ?? ['text-bg-secondary', texts.waiting];
            statusEl.className = `badge ${className}`;
            statusEl.textContent = label;
            messageEl.textContent = message;
        };

        const refreshAvailability = async () => {
            const endpoint = availability?.dataset.url;
            if (!endpoint) {
                return;
            }

            const from = arrivalInput.value;
            const to = departureInput.value;
            availabilityController?.abort();
            if (!from || !to || !(daysBetween(from, to) > 0)) {
                setAvailability('idle', texts.invalidDates);
                return;
            }

            const controller = new AbortController();
            availabilityController = controller;
            setAvailability('idle', texts.checking);

            try {
                const response = await fetch(`${endpoint}?from=${encodeURIComponent(from)}&to=${encodeURIComponent(to)}`, {
                    headers: { 'X-Requested-With': 'XMLHttpRequest' },
                    signal: controller.signal
                });
                if (!response.ok) {
                    if (controller === availabilityController) {
                        setAvailability('error', texts.loadError);
                    }
                    return;
                }

                const result = await response.json();
                if (controller !== availabilityController) {
                    return;
                }
                if (result.available === true) {
                    setAvailability('ok', texts.availableMessage);
                    return;
                }

                const blocked = Array.isArray(result.blocks) ? result.blocks.filter((b) => b.status !== 0).length : 0;
                const suffix = blocked > 0 ? ` (${blocked} ${blocked > 1 ? texts.blockingPlural : texts.blockingSingular})` : '';
                setAvailability('blocked', `${texts.unavailableMessage}${suffix}.`);
            }
            catch (err) {
                if (err?.name !== 'AbortError' && controller === availabilityController) {
                    setAvailability('error', texts.loadError);
                }
            }
        };

        const refresh = () => {
            sanitizeGuests();
            updateLinks();
            requestQuote();
            refreshAvailability();
        };

        form.addEventListener('submit', (event) => {
            if (!bookingMode) {
                event.preventDefault();
                refresh();
                return;
            }
            if (state === 'loading') {
                event.preventDefault();
                submitPending = true;
                updateSubmit();
                return;
            }
            if (state !== 'ready') {
                event.preventDefault();
                return;
            }
            // Guard against a second booking from a double click; the page is leaving anyway.
            submitting = true;
            updateSubmit();
        });

        arrivalInput.addEventListener('change', () => {
            adjustDeparture();
            refresh();
        });
        departureInput.addEventListener('change', () => {
            rememberStay();
            refresh();
        });
        guestsInput.addEventListener('change', () => {
            sanitizeGuests();
            updateLinks();
            requestQuote();
        });

        // A page restored from the back/forward cache must not keep a disabled button.
        window.addEventListener('pageshow', (event) => {
            if (event.persisted) {
                submitting = false;
                updateSubmit();
            }
        });

        adjustDeparture();
        rememberStay();
        refresh();
    };

    const start = () => document.querySelectorAll('[data-pricing-root]').forEach(initWidget);
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', start);
    }
    else {
        start();
    }
})();
