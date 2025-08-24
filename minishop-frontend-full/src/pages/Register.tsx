
import { useState } from 'react'
import api from '../lib/api'

export default function Register(){
  const [username, setUsername] = useState('alice@example.com')
  const [password, setPassword] = useState('Pa$$w0rd')
  const [email, setEmail] = useState('alice@example.com')
  const [msg, setMsg] = useState<string>('')

  async function submit(e:any){
    e.preventDefault()
    try{
      await api.post('/api/auth/register', { username, password, email })
      setMsg('Registered. You can login now.')
    }catch(err:any){
      setMsg(err?.response?.data ?? 'Register failed')
    }
  }

  return (
    <div className="card" style={{maxWidth:480}}>
      <h2>Register</h2>
      <form onSubmit={submit}>
        <div className="row"><input value={username} onChange={e=>setUsername(e.target.value)} placeholder="username"/></div>
        <div className="row"><input value={email} onChange={e=>setEmail(e.target.value)} placeholder="email"/></div>
        <div className="row"><input type="password" value={password} onChange={e=>setPassword(e.target.value)} placeholder="password"/></div>
        <button>Register</button>
      </form>
      {msg && <p>{msg}</p>}
    </div>
  )
}
