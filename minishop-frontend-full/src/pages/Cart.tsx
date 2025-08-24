import { useQuery, useQueryClient } from '@tanstack/react-query';
import api from '../lib/api';
import { getUser } from '../store/auth';
import { useMemo, useState } from 'react';

type CartRow = {
  id: number;
  userId: number;
  productId: number;
  quantity: number;
  productName?: string;
  price?: number;
  stock?: number;
};

export default function Cart() {
  const qc = useQueryClient();
  const u = getUser();
  const userId = u?.id;

  const [msg, setMsg] = useState<string>('');

  const cart = useQuery<CartRow[]>({
    enabled: !!userId,
    queryKey: ['cart', userId],
    queryFn: async () => (await api.get(`/api/cart/${userId}`)).data
  });

  async function updateQty(id: number, qty: number) {
    if (!userId) return;
    if (qty < 1) qty = 1;
    setMsg('');
    await api.put(`/api/cart/${id}`, { quantity: qty });   // <-- correct route + method
    await qc.invalidateQueries({ queryKey: ['cart', userId] });
  }

  async function removeItem(id: number) {
    if (!userId) return;
    setMsg('');
    await api.delete(`/api/cart/${id}`);                   // <-- correct route
    await qc.invalidateQueries({ queryKey: ['cart', userId] });
  }

  async function placeOrder() {
    if (!userId) return;
    setMsg('');
    // Adjust to your actual OrdersController route. Examples:
    // 1) If POST /api/orders/place/{userId}:
    // const { data } = await api.post(`/api/orders/place/${userId}`);
    // 2) If POST /api/orders/place and backend gets user from JWT:
    // const { data } = await api.post(`/api/orders/place`);
    const { data } = await api.post(`/api/orders/place/${userId}`);
    setMsg(`Comanda #${data.orderId} a fost plasată cu succes.`);
    await qc.invalidateQueries({ queryKey: ['cart', userId] });
  }

  const total = useMemo(() => {
    return (cart.data ?? []).reduce((s, r) => s + (r.price ?? 0) * r.quantity, 0);
  }, [cart.data]);

  if (!userId) return <div className="card">Te rog autentifică-te mai întâi.</div>;

  return (
    <div className="card">
      <h2>Coș</h2>

      {cart.isLoading && <p>Se încarcă…</p>}
      {cart.isError && (
        <p style={{ color: 'crimson' }}>
          Eroare la încărcarea coșului. Verifică dacă API-ul rulează și dacă ești logat.
        </p>
      )}

      {!cart.isLoading && (cart.data?.length ?? 0) === 0 && <p>Coșul este gol.</p>}

      {cart.data?.map((ci) => (
        <div key={ci.id} className="row">
          <div>
            #{ci.id} – produs {ci.productId}
            {ci.productName ? ` (${ci.productName})` : ''}
            {typeof ci.price === 'number' ? ` – ${ci.price.toFixed(2)} RON` : ''}
          </div>
          <div className="spacer" />
          <button onClick={() => updateQty(ci.id, ci.quantity - 1)}>-</button>
          <span style={{ padding: '0 8px' }}>{ci.quantity}</span>
          <button onClick={() => updateQty(ci.id, ci.quantity + 1)}>+</button>
          <button className="secondary" onClick={() => removeItem(ci.id)}>
            șterge
          </button>
        </div>
      ))}

      {cart.data && cart.data.length > 0 && (
        <>
          <div className="row" style={{ marginTop: 12 }}>
            <strong>Total estimat: </strong>
            <div className="spacer" />
            <span>{total ? `${total.toFixed(2)} RON` : '-'}</span>
          </div>
          <div className="row" style={{ marginTop: 16 }}>
            <div className="spacer" />
            <button onClick={placeOrder}>Plasează comanda</button>
          </div>
        </>
      )}

      {msg && <p style={{ marginTop: 8 }}>{msg}</p>}
    </div>
  );
}
