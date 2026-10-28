// Lectura de la Carta de Porte en la balanza de entrada:
//  - QR con la cámara (html5-qrcode) o desde una imagen
//  - Foto analizada por IA en el servidor (plan B cuando el QR no se lee)
// En ambos casos el servidor devuelve los datos y se precompleta el formulario; el operario confirma.
(function () {
    'use strict';

    const form = document.getElementById('form-ingreso');
    if (!form) return;

    const estado = document.getElementById('cpe-estado');
    const avisos = document.getElementById('cpe-avisos');
    const btnEscanear = document.getElementById('btn-escanear');
    const btnDetener = document.getElementById('btn-detener');
    const archivoQr = document.getElementById('archivo-qr');
    const archivoIa = document.getElementById('archivo-ia');
    const token = form.querySelector('input[name="__RequestVerificationToken"]').value;

    let lector = null;

    function mostrarEstado(texto) { estado.textContent = texto || ''; }

    function mostrarAvisos(lista) {
        avisos.innerHTML = '';
        (lista || []).forEach(function (a) {
            const div = document.createElement('div');
            div.textContent = '• ' + a;
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
        setValor('PatenteCamion', d.patenteCamion);
        setValor('PatenteAcoplado', d.patenteAcoplado);
        setValor('NombreChofer', d.nombreChofer);
        setValor('KilosDeclarados', d.kilosDeclarados);
        setValor('ProveedorId', d.proveedorId);
        setValor('ProductoId', d.productoId);
        mostrarAvisos(d.advertencias);
        mostrarEstado(d.origen === 'IA'
            ? 'Datos leídos con IA. Verifíquelos y registre el peso bruto.'
            : 'QR leído. Verifique los datos y registre el peso bruto.');
        form.elements['PesoBruto'].focus();
    }

    async function enviar(url, cuerpo) {
        const resp = await fetch(url, {
            method: 'POST',
            headers: { 'X-XSRF-TOKEN': token }, // nombre configurado en Program.cs (AddAntiforgery)
            body: cuerpo
        });
        const json = await resp.json().catch(function () { return null; });
        if (!resp.ok) throw new Error((json && json.error) || 'Error del servidor (' + resp.status + ')');
        return json;
    }

    async function interpretarQr(texto) {
        mostrarEstado('Interpretando QR…');
        const datos = new FormData();
        datos.append('contenido', texto);
        try {
            aplicarDatos(await enviar('/Pesajes/InterpretarQr', datos));
        } catch (e) {
            mostrarEstado('');
            mostrarAvisos([e.message]);
        }
    }

    async function detenerCamara() {
        if (lector && lector.isScanning) await lector.stop();
        btnDetener.classList.add('d-none');
        btnEscanear.disabled = false;
    }

    // --- QR con cámara ---
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
                    await interpretarQr(texto);
                },
                function () { /* sin QR en este cuadro: se ignora */ });
        } catch (e) {
            await detenerCamara();
            mostrarEstado('');
            mostrarAvisos(['No se pudo abrir la cámara (' + e + '). La cámara requiere HTTPS y permiso del navegador.']);
        }
    });

    btnDetener?.addEventListener('click', function () { detenerCamara(); mostrarEstado(''); });

    // --- QR desde una imagen ---
    archivoQr?.addEventListener('change', async function () {
        const archivo = archivoQr.files[0];
        archivoQr.value = '';
        if (!archivo) return;
        mostrarAvisos([]);
        mostrarEstado('Buscando el QR en la imagen…');
        try {
            await detenerCamara();
            lector = lector || new Html5Qrcode('lector-qr');
            const texto = await lector.scanFile(archivo, false);
            await interpretarQr(texto);
        } catch (e) {
            mostrarEstado('');
            mostrarAvisos(['No se encontró un QR legible en la imagen. Pruebe con la lectura por IA o cargue los datos a mano.']);
        }
    });

    // --- Foto con IA ---
    archivoIa?.addEventListener('change', async function () {
        const archivo = archivoIa.files[0];
        archivoIa.value = '';
        if (!archivo) return;
        mostrarAvisos([]);
        mostrarEstado('Analizando la foto con IA… (puede tardar unos segundos)');
        const datos = new FormData();
        datos.append('imagen', archivo);
        try {
            aplicarDatos(await enviar('/Pesajes/LeerCpeConIA', datos));
        } catch (e) {
            mostrarEstado('');
            mostrarAvisos([e.message]);
        }
    });

    // Patentes siempre en mayúsculas y sin espacios
    ['PatenteCamion', 'PatenteAcoplado'].forEach(function (n) {
        form.elements[n]?.addEventListener('input', function (ev) {
            ev.target.value = ev.target.value.toUpperCase().replace(/[^A-Z0-9]/g, '');
        });
    });
})();
