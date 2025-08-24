
import { useQuery, useQueryClient } from '@tanstack/react-query'
import api from '../lib/api'
import { useState } from 'react'
import { getUser } from '../store/auth'
import { Category } from '../types'

export default function AdminCategories(){
  const qc = useQueryClient()
  const u = getUser()
  const headers = u ? {'X-User-Id': u.id.toString()} : {}

  const cats = useQuery({
    queryKey: ['admin-cats'],
    queryFn: async()=> (await api.get<Category[]>('/api/admin/categories', { headers })).data
  })

  const [name, setName] = useState('')
  async function create(){
    await api.post('/api/admin/categories', { name }, { headers })
    setName('')
    qc.invalidateQueries({queryKey:['admin-cats']})
  }
  async function remove(id:number){
    await api.delete(`/api/admin/categories/${id}`, { headers })
    qc.invalidateQueries({queryKey:['admin-cats']})
  }

  return (
    <div className="card">
      <h2>Admin: Categories</h2>
      <div className="row">
        <input placeholder="name" value={name} onChange={e=>setName(e.target.value)} />
        <button onClick={create}>Create</button>
      </div>
      <table>
        <thead><tr><th>Id</th><th>Name</th><th></th></tr></thead>
        <tbody>
          {cats.data?.map(c=>(
            <tr key={c.id}><td>{c.id}</td><td>{c.name}</td><td><button className="secondary" onClick={()=>remove(c.id)}>Delete</button></td></tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
