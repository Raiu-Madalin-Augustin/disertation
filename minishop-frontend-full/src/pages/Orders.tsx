
import { useQuery } from '@tanstack/react-query'
import api from '../lib/api'
import { getUser } from '../store/auth'

export default function Orders(){
  const u = getUser()
  const userId = u?.id
  const orders = useQuery({
    enabled: !!userId,
    queryKey: ['orders', userId],
    queryFn: async()=> (await api.get(`/api/orders/user/${userId}`)).data
  })
  if(!userId) return <div className="card">Login first.</div>
  return (
    <div className="card">
      <h2>Orders</h2>
      {orders.data?.map((o:any)=>(
        <div key={o.id} className="card">
          <div>Order #{o.id} - {new Date(o.createdAt).toLocaleString()}</div>
          <ul>
            {o.items?.map((i:any,idx:number)=>(
              <li key={idx}>prod {i.productId} x {i.quantity} @ {i.price}</li>
            ))}
          </ul>
        </div>
      ))}
    </div>
  )
}
