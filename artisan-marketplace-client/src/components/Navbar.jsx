import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext.jsx';
import { useCart } from '../context/CartContext.jsx';

export default function Navbar() {
  const { user, logout } = useAuth();
  const { items } = useCart();
  const navigate = useNavigate();
  const itemCount = items.reduce((sum, i) => sum + i.quantity, 0);

  function handleLogout() {
    logout();
    navigate('/');
  }

  return (
    <header className="navbar">
      <Link to="/" className="brand">Artisan Marketplace</Link>
      <nav>
        <Link to="/products">Handcrafted Products</Link>
        <Link to="/cart">Cart ({itemCount})</Link>
        {user ? (
          <>
            <Link to="/orders">Orders</Link>
            <span>Hi, {user.fullName}</span>
            <button onClick={handleLogout}>Log out</button>
          </>
        ) : (
          <>
            <Link to="/login">Log in</Link>
            <Link to="/register">Register</Link>
          </>
        )}
      </nav>
    </header>
  );
}
