import { useQuery } from '@tanstack/react-query';
import api from '../lib/api';
import { useMemo, useState, useEffect } from 'react';
import { Product, Category } from '../types';
import { getUser } from '../store/auth';

type PagedResult<T> = {
  total: number;
  page: number;
  pageSize: number;
  items: T[];
};

function useDebounced<T>(value: T, delay = 300) {
  const [v, setV] = useState(value);
  useEffect(() => {
    const id = setTimeout(() => setV(value), delay);
    return () => clearTimeout(id);
  }, [value, delay]);
  return v;
}

export default function Catalog() {
  const [search, setSearch] = useState('');
  const [categoryId, setCategoryId] = useState<number | ''>('');
  const [page, setPage] = useState(1);
  const pageSize = 12; // tweak as you like

  const debouncedSearch = useDebounced(search.trim(), 300);

  // Reset to first page when filters change
  useEffect(() => {
    setPage(1);
  }, [debouncedSearch, categoryId]);

  // Categories (dedupe just in case backend returns duplicates)
  const cats = useQuery({
    queryKey: ['cats'],
    queryFn: async () => (await api.get<Category[]>('/api/categories')).data,
  });

  const catOptions = useMemo(() => {
    const list = Array.isArray(cats.data) ? cats.data : [];
    const byId = new Map<number, Category>();
    for (const c of list) if (!byId.has(c.id)) byId.set(c.id, c);
    return Array.from(byId.values());
  }, [cats.data]);

  // Products — match backend: GET /api/products/search -> PagedResult<Product>
  const products = useQuery({
    queryKey: ['products/search', { debouncedSearch, categoryId, page, pageSize }],
    queryFn: async () => {
      const params: Record<string, string | number> = {
        page,
        pageSize,
        sortBy: 'name',
        sortDir: 'asc',
      };
      if (debouncedSearch) params.search = debouncedSearch;
      if (categoryId !== '' && categoryId != null) params.categoryId = Number(categoryId);

      const { data } = await api.get<PagedResult<Product>>('/api/products/search', { params });
      // Normalize shape
      return {
        total: Number(data?.total ?? 0),
        page: Number(data?.page ?? page),
        pageSize: Number(data?.pageSize ?? pageSize),
        items: Array.isArray(data?.items) ? data.items : [],
      } as PagedResult<Product>;
    },
  });

  const list = products.data?.items ?? [];
  const total = products.data?.total ?? 0;
  const totalPages = Math.max(1, Math.ceil(total / pageSize));

  async function addToCart(productId: number) {
    const u = getUser();
    if (!u) { alert('Login first'); return; }
    await api.post('/api/cart/add', { userId: u.id, productId, quantity: 1 });
    alert('Added to cart');
  }

  return (
    <div>
      <div className="card row">
        <input
          placeholder="search..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
        <select
          value={categoryId === '' ? '' : String(categoryId)}
          onChange={(e) => setCategoryId(e.target.value ? Number(e.target.value) : '')}
        >
          <option value="">All categories</option>
          {catOptions.map((c) => (
            <option key={c.id} value={c.id}>{c.name}</option>
          ))}
        </select>
      </div>

      {products.isLoading && <p>Se încarcă produsele…</p>}
      {products.isError && (
        <p style={{ color: 'crimson' }}>
          Eroare la încărcarea produselor. Verifică endpoint-ul /api/products/search.
        </p>
      )}

      <div className="grid">
        {list.map((p) => (
          <div key={p.id} className="card">
            <div className="row">
              <strong>{p.name}</strong>
              <span className="spacer" />
              <span className="badge">{Number(p.price).toFixed(2)} RON</span>
            </div>
            <p>{p.description || 'No description'}</p>
            <div className="row">
              <span>Stock: {p.stock}</span>
              <span className="spacer" />
              <button onClick={() => addToCart(p.id)}>Add</button>
            </div>
          </div>
        ))}
      </div>

      {!products.isLoading && list.length === 0 && (
        <div className="card">Niciun produs găsit.</div>
      )}

      <div className="row" style={{ marginTop: 12 }}>
        <div>
          Pagina {page} / {totalPages} ({total} rezultate)
        </div>
        <div className="spacer" />
        <button
          className="secondary"
          disabled={page <= 1}
          onClick={() => setPage((p) => Math.max(1, p - 1))}
        >
          ‹ Prev
        </button>
        <button
          className="secondary"
          disabled={page >= totalPages}
          onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
          style={{ marginLeft: 8 }}
        >
          Next ›
        </button>
      </div>
    </div>
  );
}
