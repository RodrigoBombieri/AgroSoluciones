// Lectura de la Carta de Porte en la balanza de entrada. Todo corre en este equipo, sin internet y sin costo:
//  - QR con la cámara (html5-qrcode): da el CTG.
//  - PDF de la CPE: si el PDF trae texto (el original de ARCA) se lee directo con pdf.js;
//    si es un escaneo, se convierte a imagen y se lee con OCR.
//  - Foto del papel: OCR con Tesseract.js (español).
// En todos los casos también se busca el QR en el documento para cruzar el CTG.
// El servidor interpreta el texto (CpeTextoParser) y devuelve los datos; el operario confirma.
(function () {
    'use strict';

    const form = document.getElementById('form-ingreso');
    if (!form) return;

    const LIB = '/lib/';
    // px: con 2000 el OCR leyó bien la CPE real de prueba (con 1300-1800 confundía 3 con S en la patente).
    // Coincide con el ancho al que se renderiza el PDF, así no se reescala dos veces.
    const ANCHO_OCR = 2000;

    const estado = document.getElementById('cpe-estado');
    const avisos = document.getElementById('cpe-avisos');
    const btnEscanear = document.getElementById('btn-escanear');
    const btnDetener = document.getElementById('btn-detener');
    const archivoCpe = document.getElementById('archivo-cpe');
    const token = form.querySelector('input[name="__RequestVerificationToken"]').value;

    let lector = null;
    let workerOcr = null;

    // ---------------- UI ----------------

    function mostrarEstado(texto) { estado.textContent = texto || ''; }

    function mostrarAvisos(lista) {
        avisos.innerHTML = '';
        (lista || []).forEach(function (a) {
            const div = document.createElement('div');
            div.textContent = '• ' + a;
            if (a.startsWith('⚠')) div.className = 'fw-semibold text-danger';
            avisos.appendChild(div);
        });
    }

    function setValor(nombre, valor) {
        if (valor === null || valor === undefined || valor === '') return;
        const campo = form.elements[nombre];
        if (!campo) return;
        campo.value = valor;
        campo.dispatchEvent(new Event('change', { bubbles: true }));
        campo.classList.add('is-valid');
    }

    function aplicarDatos(d) {
        setValor('NumeroCPE', d.numeroCPE);
        setValor('Ctg', d.ctg);
        setValor('PatenteCamion', d.patenteCamion);
        setValor('PatenteAcoplado', d.patenteAcoplado);
        setValor('NombreChofer', d.nombreChofer);
        setValor('KilosDeclarados', d.kilosDeclarados);
        setValor('ProveedorId', d.proveedorId);
        setValor('ProductoId', d.productoId);
        mostrarAvisos(d.advertencias);
        const origen = { QR: 'QR leído', PDF: 'PDF leído', OCR: 'Documento leído con OCR' }[d.origen] || 'Datos leídos';
        mostrarEstado(origen + '. Verifique los datos contra la CPE y registre el peso bruto.');
        form.elements['PesoBruto'].focus();
    }

    async function enviar(url, campos) {
        const cuerpo = new FormData();
        Object.keys(campos).forEach(function (k) { if (campos[k] != null) cuerpo.append(k, campos[k]); });
        const resp = await fetch(url, {
            method: 'POST',
            headers: { 'X-XSRF-TOKEN': token }, // nombre configurado en Program.cs (AddAntiforgery)
            body: cuerpo
        });
        const json = await resp.json().catch(function () { return null; });
        if (!resp.ok) throw new Error((json && json.error) || 'Error del servidor (' + resp.status + ')');
        return json;
    }

    // ---------------- QR con cámara ----------------

    async function detenerCamara() {
        if (lector && lector.isScanning) await lector.stop();
        btnDetener.classList.add('d-none');
        btnEscanear.disabled = false;
    }

    btnEscanear?.addEventListener('click', async function () {
        if (typeof Html5Qrcode === 'undefined') { mostrarAvisos(['No se pudo cargar el lector de QR.']); return; }
        mostrarAvisos([]);
        lector = lector || new Html5Qrcode('lector-qr');
        btnEscanear.disabled = true;
        btnDetener.classList.remove('d-none');
        mostrarEstado('Apunte la cámara al código QR de la CPE…');
        try {
            await lector.start(
                { facingMode: 'environment' },
                { fps: 10, qrbox: { width: 250, height: 250 } },
                async function (texto) {
                    await detenerCamara();
                    mostrarEstado('Interpretando QR…');
                    try { aplicarDatos(await enviar('/Pesajes/InterpretarQr', { contenido: texto })); }
                    catch (e) { mostrarEstado(''); mostrarAvisos([e.message]); }
                },
                function () { /* sin QR en este cuadro */ });
        } catch (e) {
            await detenerCamara();
            mostrarEstado('');
            mostrarAvisos(['No se pudo abrir la cámara (' + e + '). La cámara requiere HTTPS y permiso del navegador.']);
        }
    });

    btnDetener?.addEventListener('click', function () { detenerCamara(); mostrarEstado(''); });

    // ---------------- PDF / foto ----------------

    archivoCpe?.addEventListener('change', async function () {
        const archivo = archivoCpe.files[0];
        archivoCpe.value = '';
        if (!archivo) return;
        mostrarAvisos([]);
        await detenerCamara();
        try {
            const resultado = await leerDocumento(archivo);
            mostrarEstado('Interpretando datos…');
            aplicarDatos(await enviar('/Pesajes/InterpretarTextoCpe', resultado));
        } catch (e) {
            console.error(e);
            mostrarEstado('');
            mostrarAvisos(['No se pudo leer el documento: ' + (e.message || e) + '. Complete los datos manualmente.']);
        }
    });

    /** Devuelve { texto, qr, origen } a partir de un PDF o una imagen. */
    async function leerDocumento(archivo) {
        let canvas, textoPdf = null;

        if (archivo.type === 'application/pdf' || /\.pdf$/i.test(archivo.name)) {
            mostrarEstado('Abriendo PDF…');
            const pdfjs = await import(LIB + 'pdfjs/pdf.min.mjs');
            pdfjs.GlobalWorkerOptions.workerSrc = LIB + 'pdfjs/pdf.worker.min.mjs';
            const pdf = await pdfjs.getDocument({ data: await archivo.arrayBuffer() }).promise;
            const pagina = await pdf.getPage(1);
            textoPdf = await textoDePagina(pagina);
            canvas = await renderizarPagina(pagina);
        } else {
            mostrarEstado('Cargando imagen…');
            canvas = await imagenACanvas(archivo);
        }

        mostrarEstado('Buscando el QR…');
        const qr = await leerQr(canvas);

        // PDF original de ARCA: trae el texto, no hace falta OCR
        if (textoPdf && textoPdf.replace(/\s/g, '').length > 200) {
            return { texto: textoPdf, qr: qr, origen: 'PDF' };
        }

        const texto = await ocr(prepararParaOcr(canvas));
        return { texto: texto, qr: qr, origen: 'OCR' };
    }

    /** Texto de una página de pdf.js, armado por renglones (agrupa por coordenada Y). */
    async function textoDePagina(pagina) {
        const contenido = await pagina.getTextContent();
        const renglones = new Map();
        contenido.items.forEach(function (it) {
            if (!it.str) return;
            const y = Math.round(it.transform[5] / 3) * 3;
            if (!renglones.has(y)) renglones.set(y, []);
            renglones.get(y).push({ x: it.transform[4], s: it.str });
        });
        return Array.from(renglones.keys()).sort(function (a, b) { return b - a; })
            .map(function (y) { return renglones.get(y).sort(function (a, b) { return a.x - b.x; }).map(function (i) { return i.s; }).join(' '); })
            .join('\n');
    }

    async function renderizarPagina(pagina) {
        const base = pagina.getViewport({ scale: 1 });
        // Resolución suficiente para el QR y el OCR (≈ 2000 px de ancho)
        const viewport = pagina.getViewport({ scale: 2000 / base.width });
        const canvas = document.createElement('canvas');
        canvas.width = Math.round(viewport.width);
        canvas.height = Math.round(viewport.height);
        const ctx = canvas.getContext('2d');
        ctx.fillStyle = '#fff';
        ctx.fillRect(0, 0, canvas.width, canvas.height);
        await pagina.render({ canvasContext: ctx, viewport: viewport }).promise;
        return canvas;
    }

    async function imagenACanvas(archivo) {
        const bitmap = await createImageBitmap(archivo, { imageOrientation: 'from-image' });
        const canvas = document.createElement('canvas');
        canvas.width = bitmap.width;
        canvas.height = bitmap.height;
        canvas.getContext('2d').drawImage(bitmap, 0, 0);
        return canvas;
    }

    async function leerQr(canvas) {
        if (typeof Html5Qrcode === 'undefined') return null;
        try {
            const blob = await new Promise(function (r) { canvas.toBlob(r, 'image/png'); });
            lector = lector || new Html5Qrcode('lector-qr');
            return await lector.scanFile(new File([blob], 'cpe.png', { type: 'image/png' }), false);
        } catch (e) {
            return null; // Sin QR legible: se sigue con el texto
        }
    }

    /**
     * Escala a ANCHO_OCR px, pasa a grises y estira el contraste.
     * Con las CPE reales (imágenes chicas en JPEG) esto lleva el OCR de leer mal el CUIT y la patente a leerlos bien.
     */
    function prepararParaOcr(origen) {
        const escala = ANCHO_OCR / origen.width;
        const canvas = document.createElement('canvas');
        canvas.width = ANCHO_OCR;
        canvas.height = Math.round(origen.height * escala);
        const ctx = canvas.getContext('2d', { willReadFrequently: true });
        ctx.imageSmoothingEnabled = true;
        ctx.imageSmoothingQuality = 'high';
        ctx.drawImage(origen, 0, 0, canvas.width, canvas.height);

        const img = ctx.getImageData(0, 0, canvas.width, canvas.height);
        const d = img.data;
        const grises = new Uint8ClampedArray(d.length / 4);
        const histograma = new Uint32Array(256);
        for (let i = 0, j = 0; i < d.length; i += 4, j++) {
            const g = (d[i] * 299 + d[i + 1] * 587 + d[i + 2] * 114) / 1000;
            grises[j] = g;
            histograma[grises[j]]++;
        }
        // Autocontraste: ignora el 1% más oscuro y más claro
        const total = grises.length, corte = total * 0.01;
        let min = 0, max = 255, acum = 0;
        for (let v = 0; v < 256; v++) { acum += histograma[v]; if (acum > corte) { min = v; break; } }
        acum = 0;
        for (let v = 255; v >= 0; v--) { acum += histograma[v]; if (acum > corte) { max = v; break; } }
        const rango = Math.max(1, max - min);
        for (let i = 0, j = 0; i < d.length; i += 4, j++) {
            const v = (grises[j] - min) * 255 / rango;
            d[i] = d[i + 1] = d[i + 2] = v;
        }
        ctx.putImageData(img, 0, 0);
        return canvas;
    }

    async function ocr(canvas) {
        if (typeof Tesseract === 'undefined') throw new Error('no se pudo cargar el OCR');
        if (!workerOcr) {
            mostrarEstado('Preparando lector de texto (la primera vez tarda unos segundos)…');
            workerOcr = await Tesseract.createWorker('spa', 1, {
                workerPath: LIB + 'tesseract/worker.min.js',
                corePath: LIB + 'tesseract/core',
                langPath: LIB + 'tesseract/lang',
                gzip: true,
                logger: function (m) {
                    if (m.status === 'recognizing text') mostrarEstado('Leyendo el documento… ' + Math.round(m.progress * 100) + '%');
                }
            });
        }
        mostrarEstado('Leyendo el documento…');
        const resultado = await workerOcr.recognize(canvas);
        return resultado.data.text;
    }

    // Patentes siempre en mayúsculas y sin espacios
    ['PatenteCamion', 'PatenteAcoplado'].forEach(function (n) {
        form.elements[n]?.addEventListener('input', function (ev) {
            ev.target.value = ev.target.value.toUpperCase().replace(/[^A-Z0-9]/g, '');
        });
    });
})();
