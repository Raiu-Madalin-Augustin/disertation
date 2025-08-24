import React, { useEffect, useState } from 'react';
import ReactDOM from 'react-dom/client';
import {
  createBrowserRouter,
  RouterProvider,
  NavLink,
  Navigate,
  useLocation,
  useNavigate,
} from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import './styles/app.css';

import Home from './pages/Catalog';
import Login from './pages/Login';
import Register from './pages/Register';
import Cart from './pages/Cart';
import Orders from './pages/Orders';
import AdminProducts from './pages/AdminProducts';
import AdminCategories from './pages/AdminCategories';
import AdminReports from './pages/AdminReports';

import { getUser, clearUser, isAdmin as isAdminFn, type User } from './store/auth';

const qc = new QueryClient();

/** Layout reads auth on location change so the header updates immediately after login/logout */
function Layout({ children }: { children: React.ReactNode }) {
  const location = useLocation();
  const navigate = useNavigate();
  const [user, setUser] = useState<User | null>(getUser());

  useEffect(() => {
    setUser(getUser());
  }, [location]);

  const isAdmin = isAdminFn(user);

  const handleSignOut = () => {
    clearUser();
    setUser(null);
    navigate('/login');
  };

  return (
    <>
      <header>
        <NavLink to="/">MiniShop</NavLink>
        <NavLink to="/cart">Cart</NavLink>
        <NavLink to="/orders">Orders</NavLink>

        <div className="spacer" />

        {isAdmin && (
          <>
            <NavLink to="/admin/products">Admin Products</NavLink>
            <NavLink to="/admin/categories">Admin Categories</NavLink>
            <NavLink to="/admin/reports">Reports</NavLink>
          </>
        )}

        <div className="spacer" />

        {!user ? (
          <>
            <NavLink to="/login">Login</NavLink>
            <NavLink to="/register">Register</NavLink>
          </>
        ) : (
          <>
            <span style={{ opacity: 0.8, marginRight: 8 }}>
              Hello, <strong>{user.username}</strong>
            </span>
            <button className="secondary" onClick={handleSignOut}>
              Sign out
            </button>
          </>
        )}
      </header>
      <div className="container">{children}</div>
    </>
  );
}

/** Route guard: only allow admins */
function RequireAdmin({ children }: { children: React.ReactNode }) {
  const user = getUser();
  if (!user || !isAdminFn(user)) {
    return (
      <Layout>
        <div className="card">
          <h2>Unauthorized</h2>
          <p>Ai nevoie de rol de administrator pentru a accesa această pagină.</p>
        </div>
      </Layout>
    );
  }
  return <>{children}</>;
}

/** Optional: prevent visiting /login if already logged in */
function RedirectIfAuthed({ children }: { children: React.ReactNode }) {
  const user = getUser();
  if (user) return <Navigate to="/" replace />;
  return <>{children}</>;
}

const router = createBrowserRouter([
  { path: '/', element: <Layout><Home /></Layout> },
  {
    path: '/login',
    element: (
      <RedirectIfAuthed>
        <Layout><Login /></Layout>
      </RedirectIfAuthed>
    ),
  },
  { path: '/register', element: <Layout><Register /></Layout> },
  { path: '/cart', element: <Layout><Cart /></Layout> },
  { path: '/orders', element: <Layout><Orders /></Layout> },

  // Admin-only routes
  {
    path: '/admin/products',
    element: (
      <RequireAdmin>
        <Layout><AdminProducts /></Layout>
      </RequireAdmin>
    ),
  },
  {
    path: '/admin/categories',
    element: (
      <RequireAdmin>
        <Layout><AdminCategories /></Layout>
      </RequireAdmin>
    ),
  },
  {
    path: '/admin/reports',
    element: (
      <RequireAdmin>
        <Layout><AdminReports /></Layout>
      </RequireAdmin>
    ),
  },
]);

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <QueryClientProvider client={qc}>
      <RouterProvider router={router} />
    </QueryClientProvider>
  </React.StrictMode>
);
