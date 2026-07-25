import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import client from '../api/client';
import { useAuth } from '../context/AuthContext.jsx';

export default function Orders() {
  const { user } = useAuth();
  const [orders, setOrders] = useState([]);
  const [loading, setLoading] = useState(true);
  const navigate = useNavigate();

  useEffect(() => {
    if (!user) {
      navigate('/login');
      return;
    }
    client.get('/orders').then((res) => setOrders(res.data)).finally(() => setLoading(false));
  }, [user, navigate]);

  if (loading) return <p>Loading...</p>;
  if (orders.length === 0) return <p>You haven't placed any orders yet.</p>;

  return (
    <div>
      <h2>Your orders</h2>
      {orders.map((order) => (
        <div className="card" key={order.id} style={{ marginBottom: '1rem' }}>
          <strong>Order {order.id}</strong> — {order.status}
          <p>{new Date(order.createdAt).toLocaleString()}</p>
          {order.items.map((item) => (
            <div className="cart-row" key={item.productId}>
              <span>{item.productName} × {item.quantity}</span>
              <span>${(item.unitPrice * item.quantity).toFixed(2)}</span>
            </div>
          ))}
          <p className="price">Total: ${order.totalAmount.toFixed(2)}</p>
        </div>
      ))}
    </div>
  );
}
