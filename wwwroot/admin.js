const $ = selector => document.querySelector(selector);
const form = $('#product-form');
const field = name => form.elements.namedItem(name);
const escapeHtml = value => String(value ?? '').replace(/[&<>"']/g, char => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[char]));
const safeUrl = value => { try { const url = new URL(value); return ['https:', 'http:'].includes(url.protocol) ? url.href : ''; } catch { return ''; } };
const money = value => `Gs. ${Number(value).toLocaleString('es-PY')}`;
const stock = product => (product.variants || []).reduce((sum, variant) => sum + variant.stock, 0);
let products = [], editingId = null, draftImages = [], dirty = false, busy = false;
sessionStorage.removeItem('volta-admin-key');
function message(text, error = false) { const target = $('#taxonomy-dialog').open ? $('#taxonomy-message') : $('#message'); target.textContent = text; target.className = error ? 'error' : ''; target.hidden = false; }
function sessionVisible(visible) { $('#admin-panel').hidden = !visible; $('#stats').hidden = !visible; $('#logout').hidden = !visible; $('#login-card').hidden = visible; }
async function request(url, options = {}) {
  let response;
  try { response = await fetch(url, {...options, credentials: 'same-origin'}); }
  catch { throw new Error('No se pudo conectar. Revisá tu conexión e intentá nuevamente.'); }
  if (response.status === 401) { sessionStorage.removeItem('volta-admin-key'); sessionVisible(false); throw new Error(url.endsWith('/login') ? 'Usuario o contraseña incorrectos.' : 'Tu sesión venció. Volvé a ingresar.'); }
  if (response.status === 429) throw new Error('Demasiados intentos de ingreso. Esperá un minuto y volvé a intentar.');
  if (!response.ok) {
    const text = await response.text(); let detail = text;
    try { const data = JSON.parse(text); detail = data.errors ? Object.values(data.errors).flat().join('\n') : data.detail || data.title; } catch {}
    throw new Error(detail || 'No se pudo completar la operación.');
  }
  return response.status === 204 ? null : response.json();
}
const json = (method, body) => ({method, headers: {'Content-Type':'application/json'}, body: JSON.stringify(body)});
async function run(button, action) {
  if (busy) return;
  busy = true;
  $('#admin-panel').inert = true;
  $('#admin-panel').setAttribute('aria-busy', 'true');
  if (button) button.disabled = true;
  try { await action(); } catch (error) { message(error.message, true); }
  finally { busy = false; $('#admin-panel').inert = false; $('#admin-panel').removeAttribute('aria-busy'); if (button) button.disabled = false; }
}
async function loadOptions() {
  const [brands, categories] = await Promise.all([request('/api/catalog/brands'), request('/api/catalog/categories')]);
  for (const [selector, items, label] of [['#brand-input', brands, 'Seleccionar marca'], ['#category-input', categories, 'Seleccionar categoría']]) {
    const select = $(selector), current = select.value;
    select.innerHTML = `<option value="">${label}</option>` + items.map(item => `<option value="${escapeHtml(item.id)}">${escapeHtml(item.name)}</option>`).join('');
    select.value = current;
  }
}
async function loadProducts() { products = await request('/api/admin/products?includeInactive=true'); renderProducts(); }
function renderProducts() {
  const active = products.filter(p => p.isActive);
  $('#stat-products').textContent = active.length;
  $('#stat-units').textContent = active.reduce((sum,p) => sum + stock(p), 0);
  $('#stat-low').textContent = active.filter(p => stock(p) > 0 && stock(p) <= 3).length;
  const search = $('#product-search').value.trim().toLocaleLowerCase('es');
  const filter = $('#status-filter').value;
  const shown = products.filter(p => {
    const matches = `${p.name} ${p.description} ${p.brand?.name || ''}`.toLocaleLowerCase('es').includes(search);
    const state = filter === 'all' || (filter === 'inactive' ? !p.isActive : p.isActive && (filter === 'active' || filter === 'low' && stock(p) > 0 && stock(p) <= 3 || filter === 'empty' && stock(p) === 0 || filter === 'featured' && p.isFeatured));
    return matches && state;
  });
  shown.sort((a,b) => ({name: () => a.name.localeCompare(b.name, 'es'), stock: () => stock(a)-stock(b), price: () => a.price-b.price, recent: () => new Date(b.createdAtUtc)-new Date(a.createdAtUtc)})[$('#sort-order').value]());
  $('#result-count').textContent = `${shown.length} ${shown.length === 1 ? 'producto' : 'productos'}`;
  $('#empty-state').hidden = shown.length > 0;
  $('#products').innerHTML = shown.map(p => {
    const image = p.images.find(i => i.isPrimary) || p.images[0];
    const units = stock(p);
    return `<tr><td><div class="product-info">${safeUrl(image?.cloudinaryUrl) ? `<img class="thumb" src="${escapeHtml(safeUrl(image.cloudinaryUrl))}" alt="${escapeHtml(p.name)}" loading="lazy">` : '<div class="thumb placeholder">Sin imagen</div>'}<div><span class="product-name">${escapeHtml(p.name)}</span><small>${escapeHtml(p.brand?.name)} · ${escapeHtml(p.category?.name)}</small>${p.isFeatured ? '<span class="badge featured">Destacado</span>' : ''}${!p.isActive ? '<span class="badge hidden">Oculto</span>' : ''}</div></div></td><td>${money(p.price)}</td><td><div class="inventory-count ${units === 0 ? 'is-empty' : units <= 3 ? 'is-low' : ''}"><strong>${units}</strong><span>${units === 1 ? 'unidad en total' : 'unidades en total'}</span><small>${units === 0 ? 'Sin stock' : units <= 3 ? 'Stock bajo' : 'Stock disponible'}</small></div></td><td>${p.variants.map(v => `<div class="variant-line"><div class="variant-description"><strong>${escapeHtml(v.storage)} · ${escapeHtml(v.color)}</strong><span class="variant-quantity"><b>${v.stock}</b> ${v.stock === 1 ? 'unidad disponible' : 'unidades disponibles'}</span><small>${v.price != null ? money(v.price) : 'Usa el precio base'}</small></div><div class="stock-controls"><button class="secondary" data-action="decrease" data-id="${p.id}" data-variant="${v.id}" ${v.stock === 0 ? 'disabled' : ''} aria-label="Registrar salida de ${escapeHtml(p.name)} ${escapeHtml(v.storage)} ${escapeHtml(v.color)}" title="Descontar una unidad">−1 Vendido</button><button class="secondary" data-action="increase" data-id="${p.id}" data-variant="${v.id}" aria-label="Registrar ingreso de ${escapeHtml(p.name)} ${escapeHtml(v.storage)} ${escapeHtml(v.color)}" title="Sumar una unidad">+1 Reponer</button></div></div>`).join('') || '<span class="muted">Sin variantes. Editá el producto para agregar almacenamiento, color y unidades.</span>'}</td><td><div class="row-actions"><button class="secondary" data-action="edit" data-id="${p.id}">Editar</button>${!p.isActive ? `<button class="secondary" data-action="activate" data-id="${p.id}">Publicar</button>` : ''}<button class="danger" data-action="delete" data-id="${p.id}">${p.isActive ? 'Ocultar' : 'Eliminar'}</button></div></td></tr>`;
  }).join('');
}
function addVariant(variant = {}) {
  const row = document.createElement('div'); row.className = 'variant-row';
  row.innerHTML = `<label>Almacenamiento *<input data-field="storage" value="${escapeHtml(variant.storage || '')}" placeholder="256 GB" maxlength="40" required></label><label>Color *<input data-field="color" value="${escapeHtml(variant.color || '')}" placeholder="Negro" maxlength="60" required></label><label>Unidades *<input data-field="stock" type="number" value="${variant.stock ?? 0}" min="0" max="2147483647" step="1" required></label><label>Precio propio (Gs.)<input data-field="price" type="number" value="${variant.price ?? ''}" placeholder="Precio base" min="0" max="999999999" step="1"></label><button type="button" class="danger" data-remove-variant>Quitar variante</button>`;
  $('#variants').append(row); updateTotal();
}
function updateTotal() { $('#form-stock').textContent = [...document.querySelectorAll('[data-field="stock"]')].reduce((sum,input) => sum + Math.max(0, Number(input.value) || 0), 0); }
function renderImages() {
  $('#images').innerHTML = draftImages.map((image,index) => `<div class="image-card"><img src="${escapeHtml(safeUrl(image.cloudinaryUrl))}" alt="Vista previa ${index+1}"><button type="button" class="secondary" data-primary="${index}" ${image.isPrimary ? 'disabled' : ''}>${image.isPrimary ? '★ Principal' : 'Usar como principal'}</button><button type="button" class="danger" data-remove-image="${index}">Quitar imagen</button></div>`).join('');
}
function openEditor(id = null) {
  if (busy) return;
  const product = products.find(p => p.id === id);
  editingId = id; form.reset(); $('#variants').innerHTML = '';
  for (const name of ['name','price','brandId','categoryId','description']) field(name).value = product?.[name] ?? '';
  field('isFeatured').checked = !!product?.isFeatured; field('isActive').checked = product?.isActive ?? true;
  (product?.variants.length ? product.variants : [{}]).forEach(addVariant);
  draftImages = (product?.images || []).map(image => ({...image})); renderImages();
  $('#form-title').textContent = product ? `Editar ${product.name}` : 'Nuevo producto';
  $('#submit-button').textContent = product ? 'Guardar cambios' : 'Crear producto';
  $('#inventory-view').hidden = true; $('#editor').hidden = false; $('#message').hidden = true;
  dirty = false; field('name').focus();
}
function closeEditor() {
  if (busy || dirty && !confirm('Tenés cambios sin guardar. ¿Querés descartarlos?')) return;
  dirty = false; editingId = null; $('#editor').hidden = true; $('#inventory-view').hidden = false; $('#add-product').focus();
}
$('#login-form').addEventListener('submit', event => { event.preventDefault(); run(event.submitter, async () => {
  await request('/api/admin/auth/login', json('POST', {username: $('#username').value.trim(), password: $('#password').value}));
  await Promise.all([loadOptions(), loadProducts()]); sessionVisible(true); $('#password').value = ''; $('#message').hidden = true;
}); });
$('#logout').addEventListener('click', () => {
  if (busy || dirty && !confirm('Hay cambios sin guardar. ¿Cerrar sesión y descartarlos?')) return;
  run($('#logout'), async () => {
  await request('/api/admin/auth/logout', {method:'POST'});
  dirty = false; editingId = null; form.reset(); draftImages = []; products = []; $('#products').innerHTML = '';
  $('#editor').hidden = true; $('#inventory-view').hidden = false; sessionStorage.removeItem('volta-admin-key'); sessionVisible(false); message('Sesión cerrada.');
  });
});
$('#add-product').addEventListener('click', () => openEditor());
$('#cancel-edit').addEventListener('click', closeEditor); $('#back-button').addEventListener('click', closeEditor);
$('#add-variant').addEventListener('click', () => { if (!busy) { addVariant(); dirty = true; } });
$('#variants').addEventListener('click', event => { if (busy || !event.target.closest('[data-remove-variant]')) return; if ($('#variants').children.length === 1) { message('El producto necesita al menos una variante.', true); return; } event.target.closest('.variant-row').remove(); dirty = true; updateTotal(); });
form.addEventListener('input', () => { dirty = true; updateTotal(); });
form.addEventListener('change', () => { dirty = true; });
$('#images').addEventListener('click', event => {
  if (busy) return; const button = event.target.closest('button'); if (!button) return;
  if (button.dataset.primary !== undefined) draftImages.forEach((image,index) => image.isPrimary = index === Number(button.dataset.primary));
  if (button.dataset.removeImage !== undefined) { draftImages.splice(Number(button.dataset.removeImage),1); if (draftImages.length && !draftImages.some(i => i.isPrimary)) draftImages[0].isPrimary = true; }
  dirty = true; renderImages();
});
form.addEventListener('submit', event => {
  event.preventDefault();
  run(event.submitter, async () => {
    const files = [...$('#image-files').files];
    if (files.some(file => file.size > 10*1024*1024 || !file.type.startsWith('image/'))) throw new Error('Seleccioná solo imágenes de hasta 10 MB cada una.');
    const existing = products.find(p => p.id === editingId);
    const payload = {name:field('name').value.trim(), slug:existing?.slug ?? null, price:Number(field('price').value), description:field('description').value.trim(), brandId:field('brandId').value, categoryId:field('categoryId').value, isActive:field('isActive').checked, isFeatured:field('isFeatured').checked,
      variants:[...document.querySelectorAll('.variant-row')].map(row => { const value = name => row.querySelector(`[data-field="${name}"]`).value; return {storage:value('storage').trim(),color:value('color').trim(),stock:Number(value('stock')),price:value('price') === '' ? null : Number(value('price'))}; }),
      images:draftImages.map(({cloudinaryUrl,altText,sortOrder,isPrimary}) => ({cloudinaryUrl,altText,sortOrder,isPrimary}))};
    if (!payload.name || payload.variants.some(v => !v.storage || !v.color)) throw new Error('Completá el nombre, almacenamiento y color; no pueden contener solo espacios.');
    const saved = await request(editingId ? `/api/admin/products/${editingId}` : '/api/admin/products', json(editingId ? 'PUT' : 'POST',payload));
    editingId = saved.id;
    // Record the saved state before uploads so retrying cannot create duplicate products.
    products = products.filter(p => p.id !== saved.id).concat({...saved, variants:payload.variants, images:payload.images});
    $('#form-title').textContent = `Editar ${payload.name}`; $('#submit-button').textContent = 'Guardar cambios';
    let uploadError = '';
    for (const file of files) {
      const body = new FormData(); body.append('file',file);
      try { const uploaded = await request(`/api/admin/products/${editingId}/images?isPrimary=${draftImages.length === 0}`, {method:'POST',body}); draftImages.push({...uploaded,sortOrder:0,altText:null}); }
      catch (error) { uploadError = `El producto se guardó, pero no se pudo subir «${file.name}»: ${error.message} Seleccioná nuevamente las imágenes pendientes.`; break; }
    }
    $('#image-files').value = ''; renderImages(); dirty = false;
    try { await loadProducts(); } catch (error) { message(`El producto se guardó. No se pudo actualizar la lista: ${error.message}`, true); return; }
    if (uploadError) { message(uploadError, true); return; }
    $('#editor').hidden = true; $('#inventory-view').hidden = false; editingId = null;
    message('Producto guardado correctamente.'); $('#add-product').focus();
  });
});
$('#products').addEventListener('click', event => {
  const button = event.target.closest('button[data-action]'); if (!button || busy) return;
  const {action,id,variant} = button.dataset, product = products.find(p => p.id === id);
  if (action === 'edit') { openEditor(id); return; }
  if (action === 'delete' && !confirm(product.isActive ? `¿Ocultar «${product.name}» de la tienda? Vas a conservar sus imágenes y existencias.` : `¿Eliminar definitivamente «${product.name}» y sus variantes e imágenes del inventario? Esta acción no se puede deshacer.`)) return;
  run(button, async () => {
    let text;
    if (action === 'delete') { await request(`/api/admin/products/${id}`, {method:'DELETE'}); text = product.isActive ? 'Producto oculto. Podés encontrarlo en el filtro «Ocultos».' : 'Producto eliminado definitivamente.'; }
    else if (action === 'activate') { await request(`/api/admin/products/${id}/activate`, {method:'POST'}); text = 'Producto publicado en la tienda.'; }
    else { const result = await request(`/api/admin/products/${id}/variants/${variant}/stock/${action}`, json('POST',{quantity:1})); text = `${action === 'increase' ? 'Ingreso' : 'Salida'} registrado: ${product.name}. Quedan ${result.stock} unidades en esta variante.`; }
    await loadProducts(); message(text);
  });
});
for (const [selector,type] of [['#product-search','input'],['#status-filter','change'],['#sort-order','change']]) $(selector).addEventListener(type,renderProducts);
$('#open-taxonomy').addEventListener('click', () => {
  if (busy) return;
  $('#taxonomy-message').hidden = true;
  $('#taxonomy-dialog').showModal();
});
$('#close-taxonomy').addEventListener('click', () => { if (!busy) $('#taxonomy-dialog').close(); });
$('#taxonomy-dialog').addEventListener('cancel', event => { if (busy) event.preventDefault(); });
for (const [selector,endpoint,label] of [['#brand-form','brands','Marca'],['#category-form','categories','Categoría']]) $(selector).addEventListener('submit',event => {
  event.preventDefault(); const name = new FormData(event.target).get('name').trim();
  run(event.submitter,async () => { if (!name) throw new Error('Ingresá un nombre.'); await request(`/api/catalog/${endpoint}`,json('POST',{name})); event.target.reset(); await loadOptions(); message(`${label} agregada.`); });
});
window.addEventListener('beforeunload',event => { if (dirty || busy) { event.preventDefault(); event.returnValue = ''; } });
sessionVisible(false);
window.addEventListener('pagehide', () => {
  sessionVisible(false);
  $('#taxonomy-dialog').close();
});
window.addEventListener('pageshow', event => {
  if (event.persisted) window.location.reload();
});

