// src/store/auth.ts
export type Role = 'Admin' | 'Client';

export type User = {
  id: number;
  username: string;
  email?: string;
  role: Role;
  isAdmin?: boolean;
};

const LS_KEY = 'ms_user';

function normalizeUser(raw: any): User {
  const id =
    Number(raw?.id ?? raw?.userId ?? raw?.UserId ?? raw?.Id ?? 0);

  const username =
    String(raw?.username ?? raw?.userName ?? raw?.Username ?? raw?.name ?? raw?.Name ?? '');

  // Prefer explicit role, else infer from isAdmin, else default to Client
  let role: Role = 'Client';
  const roleRaw = raw?.role ?? raw?.Role ?? raw?.roleName ?? raw?.RoleName;
  if (typeof roleRaw === 'string') {
    role = roleRaw.toLowerCase().includes('admin') ? 'Admin' : 'Client';
  } else if (raw?.isAdmin === true || raw?.IsAdmin === true) {
    role = 'Admin';
  }

  const email =
    raw?.email ?? raw?.Email ?? (username.includes('@') ? username : undefined);

  return { id, username: username || (email ?? ''), email, role, isAdmin: role === 'Admin' };
}

export function setUser(raw: any) {
  const u = normalizeUser(raw);
  if (!u.id || !u.username) {
    // don’t save junk – make it obvious in console
    console.warn('Auth: login response missing id/username; not saving', raw);
    clearUser();
    return;
  }
  try {
    localStorage.setItem(LS_KEY, JSON.stringify(u));
  } catch (e) {
    console.error('Auth: failed to save user to localStorage', e);
  }
}

export function getUser(): User | null {
  try {
    const s = localStorage.getItem(LS_KEY);
    if (!s) return null;
    const parsed = JSON.parse(s);
    // ensure shape still valid
    if (!parsed?.id || !parsed?.username) return null;
    return parsed as User;
  } catch {
    return null;
  }
}

export function clearUser() {
  try {
    localStorage.removeItem(LS_KEY);
  } catch {}
}

export function isAdmin(u?: User | null): boolean {
  if (!u) return false;
  if (u.isAdmin === true) return true;
  return String(u.role).toLowerCase() === 'admin';
}
