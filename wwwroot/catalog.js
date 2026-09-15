const WHATSAPP_PHONE = '595991408440';
const state = { products: [], brands: [], categories: [], whatsappPhone: WHATSAPP_PHONE };
const money = value => new Intl.NumberFormat('es-PY', { style: 'currency', currency: 'PYG', maximumFractionDigits: 0 }).format(value);
const $ = selector => document.querySelector(selector);

function renderProductCard(product) {
  return `<article class="product"><div class="product-image">${product.primaryImageUrl ? `<img src="${product.primaryImageUrl}" alt="${product.name}">` : `<span class="product-placeholder">${product.brand.slice(0, 2).toUpperCase()}</span>`}${product.isFeatured ? '<span class="tag">Destacado</span>' : ''}</div><div class="product-info"><p>${product.brand} / ${product.category}</p><h3>${product.name}</h3><div class="price-row"><span class="price">${product.hasPriceRange ? 'Desde ' : ''}${money(product.price)}</span><button class="ask" data-id="${product.id}">Ver opciones →</button></div></div></article>`;
}

function getFilteredProducts() {
  const search = ($('#search').value || '').toLowerCase();
  const brand = $('#brand').value || '';
  const category = $('#category').value || '';
  const sort = $('#sort').value || 'featured';
  const products = state.products.filter(product => product.name.toLowerCase().includes(search) && (!brand || product.brandId === brand) && (!category || product.categoryId === category));
  if (sort === 'low') products.sort((first, second) => first.price - second.price);
  if (sort === 'high') products.sort((first, second) => second.price - first.price);
  if (sort === 'featured') products.sort((first, second) => Number(second.isFeatured) - Number(first.isFeatured) || first.name.localeCompare(second.name));
  return products;
}

function bindProductInteractions(products) {
  document.querySelectorAll('.ask').forEach(button => button.addEventListener('click', event => {
    event.stopPropagation();
    const product = state.products.find(item => item.id === button.dataset.id);
    if (!product) return;
    window.openProductDetails(product.id);
  }));
  document.querySelectorAll('.product').forEach((card, index) => {
    card.style.cursor = 'pointer';
    card.addEventListener('click', () => openGallery(products[index].id, products[index].primaryImageUrl));
    const image = card.querySelector('.product-image');
    if (image) image.addEventListener('click', event => {
      event.stopPropagation();
      openGallery(products[index].id, products[index].primaryImageUrl);
    });
  });
}

function render() {
  const products = getFilteredProducts();
  $('#count').textContent = `${products.length} equipos disponibles`;
  $('#products').innerHTML = products.map(renderProductCard).join('') || '<div class="empty">No encontramos equipos con esos filtros.</div>';
  bindProductInteractions(products);
}

function openGallery(productId) { window.openProductDetails(productId); }

async function load() {
  try {
    const [products, brands, categories] = await Promise.all([fetch('/api/products').then(response => response.json()), fetch('/api/catalog/brands').then(response => response.json()), fetch('/api/catalog/categories').then(response => response.json())]);
    state.products = products;
    state.brands = brands;
    state.categories = categories;
    document.querySelectorAll('a[href^="https://wa.me/"]').forEach(link => link.href = `https://wa.me/${WHATSAPP_PHONE}`);
    $('#brand').innerHTML += brands.map(brand => `<option value="${brand.id}">${brand.name}</option>`).join('');
    $('#category').innerHTML += categories.map(category => `<option value="${category.id}">${category.name}</option>`).join('');
    render();
  } catch {
    $('#count').textContent = 'No se pudo cargar el catálogo';
    $('#products').innerHTML = '<div class="empty">El catálogo estará disponible cuando se configure la base de datos.</div>';
  }
}

$('#lightbox-close').addEventListener('click', () => $('#lightbox').classList.add('hidden'));
$('#lightbox').addEventListener('click', event => { if (event.target.id === 'lightbox') $('#lightbox').classList.add('hidden'); });
['search', 'brand', 'category', 'sort'].forEach(id => $(`#${id}`).addEventListener(id === 'search' ? 'input' : 'change', render));
$('#clear').addEventListener('click', () => { $('#search').value = ''; $('#brand').value = ''; $('#category').value = ''; $('#sort').value = 'featured'; render(); });
load();
