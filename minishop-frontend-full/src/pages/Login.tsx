// src/pages/Login.tsx
import { useState } from 'react';
import api from '../lib/api';
import { setUser } from '../store/auth';
import { useNavigate } from 'react-router-dom';

export default function Login(){
  const [username, setUsername] = useState('admin@example.com');
  const [password, setPassword] = useState('Admin123!');
  const [msg, setMsg] = useState<string>('');
  const nav = useNavigate();

  function toMessage(err: any): string {
    const data = err?.response?.data;
    if (!data) return 'Login failed';
    if (typeof data === 'string') return data;
    if (typeof data?.title === 'string') return data.title;
    if (typeof data?.message === 'string') return data.message;
    return 'Login failed';
  }

  async function submit(e: React.FormEvent){
    e.preventDefault();
    setMsg('');
    try{
      const { data } = await api.post('api/auth/login', {
  email: username,   // <= send as `email`
  password
});

      setUser(data);            // <-- persistă userul în localStorage
      setMsg('Autentificare reușită');
      nav('/');                 // redirect
    }catch(err: any){
      setMsg(toMessage(err));
    }
  }

  return (
    <div className="card" style={{maxWidth:480}}>
      <h2>Login</h2>
      <form onSubmit={submit}>
        <div className="row">
          <input value={username} onChange={e=>setUsername(e.target.value)} placeholder="username" />
        </div>
        <div className="row">
          <input type="password" value={password} onChange={e=>setPassword(e.target.value)} placeholder="password" />
        </div>
        <button>Login</button>
      </form>
      {msg && <p>{msg}</p>}
    </div>
  );
}
