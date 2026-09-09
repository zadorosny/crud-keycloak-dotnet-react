export type Me = {
  id: string;
  email: string | null;
  username: string | null;
  roles: string[];
};

export type Product = {
  id: string;
  name: string;
  description: string | null;
  price: number;
  stock: number;
  createdById: string;
  createdAt: string;
  updatedAt: string;
};

export type ProductRequest = {
  name: string;
  description: string | null;
  price: number;
  stock: number;
};

export type PagedResult<T> = {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
};

export type TwoFactorDevice = {
  id: string;
  label: string | null;
  createdAt: string;
};

export type TwoFactorStatus = {
  enabled: boolean;
  devices: TwoFactorDevice[];
};

export type User = {
  id: string;
  email: string | null;
  username: string | null;
  firstName: string | null;
  lastName: string | null;
  enabled: boolean;
  roles: string[];
  twoFactorEnabled: boolean;
  createdAt: string;
};

export type UserPage = {
  items: User[];
  first: number;
  max: number;
};
