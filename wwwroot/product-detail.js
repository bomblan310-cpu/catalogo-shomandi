(() => {
  const dialog = document.createElement('dialog');
  dialog.className = 'product-detail';
  dialog.setAttribute('aria-labelledby', 'detail-title');
  dialog.innerHTML = `<button type="button" class="detail-close" aria-label="Cerrar detalle">✕</button><div id="detail-content"></div>`;
  document.body.append(dialog);
  const content = dialog.querySelector('#detail-content');
  const escape = value => String(value ?? '').replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
  const money = value => new Intl.NumberFormat('es-PY', {style:'currency',currency:'PYG',maximumFractionDigits:0}).format(value);
  const imageUrl = value => { try { const url = new URL(value); return ['https:','http:'].includes(url.protocol) ? escape(url.href) : ''; } catch { return ''; } };
  let generation = 0;
  dialog.querySelector('.detail-close').onclick = () => dialog.close();
  dialog.addEventListener('click', event => { if (event.target === dialog) { const rect=dialog.getBoundingClientRect(); if(event.clientX<rect.left||event.clientX>rect.right||event.clientY<rect.top||event.clientY>rect.bottom) dialog.close(); } });
  dialog.addEventListener('close', () => { generation++; });
  async function read(url) { const response=await fetch(url); if(!response.ok) throw new Error('No pudimos cargar este producto. Cerrá esta ventana e intentá nuevamente.'); return response.json(); }
  window.openProductDetails = async id => {
    const current = ++generation;
    content.innerHTML = '<h2 id="detail-title">Cargando producto…</h2><p role="status">Consultando precios y disponibilidad.</p>';
    if (!dialog.open) dialog.showModal();
    try {
      const [product, contact] = await Promise.all([read(`/api/products/${encodeURIComponent(id)}`),read('/api/catalog/contact')]);
      if(current !== generation || !dialog.open) return;
      const variants = product.variants || [];
      const images = [...(product.images || [])].sort((a,b)=>Number(b.isPrimary)-Number(a.isPrimary)||a.sortOrder-b.sortOrder);
      content.innerHTML = `<div class="detail-layout"><div class="detail-gallery">${images.length ? images.map(image=>`<img src="${imageUrl(image.cloudinaryUrl)}" alt="${escape(image.altText || product.name)}">`).join('') : '<div class="detail-placeholder">Sin imagen disponible</div>'}</div><div class="detail-info"><p class="eyebrow">${escape(product.brand?.name)} / ${escape(product.category?.name)}</p><h2 id="detail-title">${escape(product.name)}</h2><p class="detail-description">${escape(product.description)}</p><div class="detail-options"><label>Almacenamiento<select id="detail-storage"></select></label><label>Color<select id="detail-color"></select></label></div><div class="detail-summary" aria-live="polite"><strong id="detail-price"></strong><p id="detail-stock"></p></div><button type="button" id="detail-add" class="detail-whatsapp detail-add" disabled>Agregar al carrito</button><a id="detail-whatsapp" class="detail-whatsapp" target="_blank" rel="noopener noreferrer" aria-disabled="true">Consultar esta variante por WhatsApp ↗</a><p id="detail-error" role="status"></p></div></div>`;
      const storage = dialog.querySelector('#detail-storage'), color = dialog.querySelector('#detail-color');
      const link = dialog.querySelector('#detail-whatsapp');
      link.addEventListener('click', event => { if(link.getAttribute('aria-disabled') === 'true') event.preventDefault(); });
      const add = dialog.querySelector('#detail-add');
      add.onclick = () => { try { window.storeCart.add(product, variants.find(v => v.id === color.value)); dialog.querySelector('#detail-error').textContent = 'Agregado al carrito. Cerrá esta ventana para seguir eligiendo o abrir tu carrito.'; } catch(error) { dialog.querySelector('#detail-error').textContent = error.message; } };
      let selection = 0;
      async function updateSelection() {
        const turn = ++selection;
        const variant = variants.find(v=>v.id === color.value);
        dialog.querySelector('#detail-price').textContent = money(variant?.price ?? product.price);
        dialog.querySelector('#detail-stock').textContent = variant ? (variant.stock > 0 ? `${variant.stock} ${variant.stock === 1 ? 'unidad disponible' : 'unidades disponibles'}` : 'Sin stock en esta combinación') : 'Sin variantes disponibles';
        add.disabled = !variant || variant.stock <= 0;
        link.removeAttribute('href'); link.setAttribute('aria-disabled','true');
        dialog.querySelector('#detail-error').textContent = '';
        if (!variant || variant.stock <= 0) return;
        try {
          const phone = String(contact.whatsAppPhone || '').replace(/\D/g,'');
          if(!phone) throw new Error('No se pudo obtener el contacto de la tienda.');
          const result = await read(`/api/products/${encodeURIComponent(id)}/whatsapp?phone=${encodeURIComponent(phone)}&variantId=${encodeURIComponent(variant.id)}`);
          if(current !== generation || turn !== selection || !dialog.open) return;
          const url = new URL(result.url);
          if(url.protocol !== 'https:' || url.hostname !== 'wa.me') throw new Error('No se pudo preparar la consulta por WhatsApp.');
          link.href = url.href; link.setAttribute('aria-disabled','false');
        } catch(error) { if(current === generation && turn === selection && dialog.open) dialog.querySelector('#detail-error').textContent=error.message; }
      }
      function updateColors() {
        const matching = variants.filter(v=>v.storage === storage.value);
        color.innerHTML = matching.map(v=>`<option value="${escape(v.id)}">${escape(v.color)}${v.stock <= 0 ? ' · Sin stock' : ''}</option>`).join('');
        const available = matching.find(v=>v.stock>0); if(available) color.value=available.id;
        updateSelection();
      }
      storage.innerHTML = [...new Set(variants.map(v=>v.storage))].map(value=>`<option value="${escape(value)}">${escape(value)}</option>`).join('');
      const available = variants.find(v=>v.stock>0); if(available) storage.value=available.storage;
      storage.disabled = color.disabled = !variants.length;
      storage.addEventListener('change',updateColors); color.addEventListener('change',updateSelection);
      updateColors();
    } catch(error) { if(current === generation && dialog.open) content.innerHTML=`<h2 id="detail-title">No se pudo abrir el producto</h2><p role="alert">${escape(error.message)}</p>`; }
  };
})();
