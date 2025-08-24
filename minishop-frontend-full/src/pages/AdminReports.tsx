import { useQuery } from '@tanstack/react-query';
import api from '../lib/api';
import { getUser } from '../store/auth';
import React from 'react';

type SalesByCategory = {
  categoryId: number;
  categoryName: string;
  totalQty: number;
  totalRevenue: number;
};

type SalesByCategoryResponse = {
  from: string;           // ISO datetime from backend
  to: string;             // ISO datetime from backend
  rows: SalesByCategory[];
  totalRevenue: number;   // grand total
  totalQty: number;       // grand total qty
};

export default function AdminReports() {
  const u = getUser();
  // Only send header if your backend actually reads it; harmless otherwise.
  const headers = u ? { 'X-User-Id': u.id.toString() } : {};

  const sales = useQuery<SalesByCategoryResponse>({
    queryKey: ['sales-by-category'],
    queryFn: async () =>
      (await api.get<SalesByCategoryResponse>('/api/admin/reports/sales-by-category', { headers })).data,
    enabled: true,
  });

  const rows = sales.data?.rows ?? [];

  return (
    <div className="card">
      <h2>Reports: Sales by Category</h2>

      {sales.isLoading && <p>Se încarcă…</p>}
      {sales.isError && (
        <p style={{ color: 'crimson' }}>
          Eroare la încărcarea raportului. Verifică API-ul și permisiunile.
        </p>
      )}

      {!sales.isLoading && !sales.isError && (
        <>
          <div style={{ marginBottom: 8, fontSize: 14, opacity: 0.8 }}>
            <span>
              Interval:&nbsp;
              <strong>
                {sales.data?.from ? new Date(sales.data.from).toLocaleString() : '-'}
              </strong>
              &nbsp;→&nbsp;
              <strong>
                {sales.data?.to ? new Date(sales.data.to).toLocaleString() : '-'}
              </strong>
            </span>
          </div>

          <table>
            <thead>
              <tr>
                <th style={{ textAlign: 'left' }}>Category</th>
                <th style={{ textAlign: 'right' }}>Total Qty</th>
                <th style={{ textAlign: 'right' }}>Total Revenue (RON)</th>
              </tr>
            </thead>
            <tbody>
              {rows.map((r, idx) => {
                const qty = Number(r.totalQty ?? 0);
                const revenue = Number(r.totalRevenue ?? 0);
                return (
                  <tr key={r.categoryId ?? idx}>
                    <td>{r.categoryName ?? '-'}</td>
                    <td style={{ textAlign: 'right' }}>{isFinite(qty) ? qty : '-'}</td>
                    <td style={{ textAlign: 'right' }}>
                      {isFinite(revenue) ? revenue.toFixed(2) : '-'}
                    </td>
                  </tr>
                );
              })}
              {rows.length === 0 && (
                <tr>
                  <td colSpan={3} style={{ textAlign: 'center', padding: 12 }}>
                    No data for the selected interval.
                  </td>
                </tr>
              )}
            </tbody>
            <tfoot>
              <tr>
                <th style={{ textAlign: 'right' }}>Totals:</th>
                <th style={{ textAlign: 'right' }}>
                  {isFinite(Number(sales.data?.totalQty))
                    ? Number(sales.data?.totalQty).toString()
                    : '-'}
                </th>
                <th style={{ textAlign: 'right' }}>
                  {isFinite(Number(sales.data?.totalRevenue))
                    ? Number(sales.data?.totalRevenue).toFixed(2)
                    : '-'}
                </th>
              </tr>
            </tfoot>
          </table>
        </>
      )}
    </div>
  );
}
