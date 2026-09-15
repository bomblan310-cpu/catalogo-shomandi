const WHATSAPP_PHONE = '595991408440';
const state = { products: [], brands: [], whatsappPhone: WHATSAPP_PHONE, featuredIndex: 0 };
const money = value => new Intl.NumberFormat('es-PY', { style: 'currency', currency: 'PYG', maximumFractionDigits: 0 }).format(value);
const $ = selector => document.querySelector(selector);

function renderProductCard(product) {
  return `<article class="product"><div class="product-image">${product.primaryImageUrl ? `<img src="${product.primaryImageUrl}" alt="${product.name}">` : `<span class="product-placeholder">${product.brand.slice(0, 2).toUpperCase()}</span>`}${product.isFeatured ? '<span class="tag">Destacado</span>' : ''}</div><div class="product-info"><p>${product.brand} / ${product.category}</p><h3>${product.name}</h3><div class="price-row"><span class="price">${product.hasPriceRange ? 'Desde ' : ''}${money(product.price)}</span><button class="ask" data-id="${product.id}">Ver opciones →</button></div></div></article>`;
}

function renderHeroFeatured() {
  const featured = state.products.filter(product => product.isFeatured);
  const visual = $('#hero-featured-visual');
  if (!visual) return;
  if (!featured.length) {
    document.querySelector('.home-featured-caption').hidden = true;
    visual.innerHTML = '<div class="hero-featured-placeholder">SHOMANDI</div>';
    return;
  }
  const product = featured[state.featuredIndex % featured.length];
  document.querySelector('.home-featured-caption').hidden = false;
  $('#hero-product-name').textContent = product.name;
  $('#hero-product-price').textContent = `${product.hasPriceRange ? 'Desde ' : ''}${money(product.price)}`;
  $('#hero-product-open').onclick = () => openGallery(product.id);
  visual.innerHTML = product.primaryImageUrl
    ? `<img src="${product.primaryImageUrl}" alt="${product.name}">`
    : `<div class="hero-featured-placeholder">${product.brand.slice(0, 2).toUpperCase()}</div>`;
  visual.style.cursor = 'pointer';
  visual.onclick = () => openGallery(product.id, product.primaryImageUrl);
}

function bindProductInteractions(products, rootSelector) {
  const root = document.querySelector(rootSelector);
  if (!root) return;
  root.querySelectorAll('.ask').forEach(button => button.addEventListener('click', event => {
    event.stopPropagation();
    const product = state.products.find(item => item.id === button.dataset.id);
    if (!product) return;
    window.openProductDetails(product.id);
  }));
  root.querySelectorAll('.product-image').forEach((element, index) => element.addEventListener('click', () => openGallery(products[index].id)));
}

async function load() {
  try {
    const products = await fetch('/api/products').then(response => response.json());
    state.products = products;
    render();
  } catch {
    const count = $('#count');
    const products = $('#products');
    if (count) count.textContent = 'Conectá PostgreSQL para ver el catálogo';
    if (products) products.innerHTML = '<div class="empty">El catálogo estará disponible cuando se configure la base de datos.</div>';
  }
}

function getCatalogProducts() {
  const search = ($('#search').value || '').toLowerCase();
  const brand = $('#brand').value || '';
  const sort = $('#sort').value || 'featured';
  let products = state.products.filter(product => product.name.toLowerCase().includes(search) && (!brand || product.brandId === brand));
  if (sort === 'low') products.sort((a, b) => a.price - b.price);
  if (sort === 'high') products.sort((a, b) => b.price - a.price);
  if (sort === 'featured') products.sort((a, b) => Number(b.isFeatured) - Number(a.isFeatured) || a.name.localeCompare(b.name));
  return products;
}

function renderFeatured() {
  const featured = state.products.filter(product => product.isFeatured);
  const container = document.querySelector('#featured-products');
  if (!container) return;
  container.innerHTML = featured.map(renderProductCard).join('') || '<div class="empty">Próximamente tendremos más destacados.</div>';
  bindProductInteractions(featured, '#featured-products');
}

function renderCatalog() {
  const products = getCatalogProducts();
  $('#count').textContent = `${products.length} equipos disponibles`;
  $('#products').innerHTML = products.map(renderProductCard).join('') || '<div class="empty">No encontramos equipos con esos filtros.</div>';
  bindProductInteractions(products, '#products');
}

function updateCatalogVisibility() {
  const catalog = $('#catalogo');
  if (!catalog) return;
  const isCatalogRoute = window.location.hash === '#catalogo';
  catalog.classList.toggle('catalog-hidden', !isCatalogRoute);
  if (isCatalogRoute) requestAnimationFrame(() => catalog.scrollIntoView({ behavior: 'smooth', block: 'start' }));
}

function render() {
  renderHeroFeatured();
}

function openGallery(productId) { window.openProductDetails(productId); }

$('#lightbox-close').addEventListener('click', () => $('#lightbox').classList.add('hidden'));
$('#lightbox').addEventListener('click', event => { if (event.target.id === 'lightbox') $('#lightbox').classList.add('hidden'); });

['search', 'brand', 'sort'].forEach(id => {
  const control = $(`#${id}`);
  if (control) control.addEventListener(id === 'search' ? 'input' : 'change', render);
});
const clear = $('#clear');
if (clear) clear.addEventListener('click', () => { $('#search').value = ''; $('#brand').value = ''; $('#sort').value = 'featured'; render(); });
const featuredCard = $('#hero-featured');
const motionPreference = window.matchMedia('(prefers-reduced-motion: reduce)');
let autoplayPaused = motionPreference.matches;
let pointerOverFeatured = false;
function updateAutoplayPreference() {
  document.querySelector('.home-featured-caption').setAttribute('aria-live', autoplayPaused ? 'polite' : 'off');
}
motionPreference.addEventListener('change', event => { autoplayPaused = event.matches; updateAutoplayPreference(); });
featuredCard.addEventListener('pointerenter', event => { if (event.pointerType !== 'touch') pointerOverFeatured = true; });
featuredCard.addEventListener('pointerleave', () => { pointerOverFeatured = false; });
setInterval(() => {
  if (autoplayPaused || pointerOverFeatured || document.hidden || featuredCard.contains(document.activeElement) || document.querySelector('dialog[open]')) return;
  const bounds = featuredCard.getBoundingClientRect();
  if (bounds.bottom <= 0 || bounds.top >= window.innerHeight) return;
  const total = state.products.filter(product => product.isFeatured).length;
  if (total < 2) return;
  state.featuredIndex = (state.featuredIndex + 1) % total;
  renderHeroFeatured();
}, 5000);
updateAutoplayPreference();
load();

