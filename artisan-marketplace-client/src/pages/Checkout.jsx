import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import client from '../api/client';
import { useCart } from '../context/CartContext.jsx';
import { useAuth } from '../context/AuthContext.jsx';

export default function Checkout() {
  const { items, total, clearCart } = useCart();
  const { user } = useAuth();
  const [placing, setPlacing] = useState(false);
  const [error, setError] = useState('');
  const [orderId, setOrderId] = useState(null);
  const navigate = useNavigate();

  if (!user) {
    navigate('/login');
    return null;
  }

  async function placeOrder() {
    setPlacing(true);
    setError('');
    try {
      const { data } = await client.post('/orders', { items });
      setOrderId(data.id);
      clearCart();
    } catch (err) {
      setError('Could not place order. Please try again.');
    } finally {
      setPlacing(false);
    }
  }

  if (orderId) {
    return (
      <div>
        <h2>Order placed!</h2>
        <p>Order ID: {orderId}</p>
        <p>This is a demo checkout — no real payment was charged (no Stripe call yet).</p>
      </div>
    );
  }

  return (
    <div>
      <h2>Checkout</h2>
      {items.map((item) => (
        <div className="cart-row" key={item.productId}>
          <span>{item.productName} × {item.quantity}</span>
          <span>${(item.unitPrice * item.quantity).toFixed(2)}</span>
        </div>
      ))}
      <h3>Total: ${total.toFixed(2)}</h3>
      {error && <p className="error">{error}</p>}
      <button className="btn" onClick={placeOrder} disabled={placing}>
        {placing ? 'Placing order...' : 'Place order'}
      </button>
    </div>
  );
}
