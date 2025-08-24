
import { useQuery, useQueryClient } from '@tanstack/react-query'
import api from '../lib/api'
import { useState } from 'react'
import { getUser } from '../store/auth'
import { Product, Category } from '../types'

export default function AdminProducts(){
  const qc = useQueryClient()
  const u = getUser()
  const headers = u ? {'X-User-Id': u.id.toString()} : {}

  const products = useQuery({
    queryKey: ['admin-products'],
    queryFn: async()=> (await api.get<Product[]>('/api/admin/products', { headers })).data
  })
  const cats = useQuery({
    queryKey: ['cats'],
    queryFn: async()=> (await api.get<Category[]>('/api/categories')).data
  })

  const [form,setForm] = useState<Partial<Product>>({ name:'', price:0, stock:0, categoryId: 0 })

  async function create(){
    await api.post('/api/admin/products', form, { headers })
    setForm({ name:'', price:0, stock:0, categoryId: 0 })
    qc.invalidateQueries({queryKey:['admin-products']})
  }
  async function remove(id:number){
    await api.delete(`/api/admin/products/${id}`, { headers })
    qc.invalidateQueries({queryKey:['admin-products']})
  }

  return (
    <div>
      <div className="card">
        <h2>Admin: Products</h2>
        <div className="row">
          <input placeholder="name" value={form.name||''} onChange={e=>setForm({...form, name:e.target.value})}/>
          <input placeholder="price" type="number" value={form.price||0} onChange={e=>setForm({...form, price:Number(e.target.value)})}/>
          <input placeholder="stock" type="number" value={form.stock||0} onChange={e=>setForm({...form, stock:Number(e.target.value)})}/>
          <select value={form.categoryId||0} onChange={e=>setForm({...form, categoryId:Number(e.target.value)})}>
            <option value={0}>--category--</option>
            {cats.data?.map(c=> <option key={c.id} value={c.id}>{c.name}</option>)}
          </select>
          <button onClick={create}>Create</button>
        </div>
      </div>

      <div className="grid">
        {products.data?.map(p=>(
          <div key={p.id} className="card">
            <strong>{p.name}</strong>
            <div className="row"><span>${p.price}</span><span className="spacer"/><span>Stock: {p.stock}</span></div>
            <div className="row"><button className="secondary" onClick={()=>remove(p.id)}>Delete</button></div>
          </div>
        ))}
      </div>
    </div>
  )
}
