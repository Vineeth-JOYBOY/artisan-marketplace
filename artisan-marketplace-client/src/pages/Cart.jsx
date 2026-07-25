import { Link, useNavigate } from 'react-router-dom';
import { useCart } from '../context/CartContext.jsx';
import { useAuth } from '../context/AuthContext.jsx';

export default function Cart() {
  const { items, removeFromCart, updateQuantity, total } = useCart();
  const { user } = useAuth();
  const navigate = useNavigate();

  if (items.length === 0) {
    return (
      <div>
        <h2>Your cart is empty</h2>
        <Link to="/products" className="btn">Browse products</Link>
      </div>
    );
  }

  function handleCheckout() {
    navigate(user ? '/checkout' : '/login');
  }

  return (
    <div>
      <h2>Your cart</h2>
      {items.map((item) => (
        <div className="cart-row" key={item.productId}>
          <div>
            <strong>{item.productName}</strong>
            <div>
              <input
                type="number"
                min="1"
                value={item.quantity}
                onChange={(e) => updateQuantity(item.productId, Number(e.target.value))}
                style={{ width: 70 }}
              />
              {' '}× ${item.unitPrice.toFixed(2)}
            </div>
          </div>
          <div>
            <span className="price">${(item.unitPrice * item.quantity).toFixed(2)}</span>
            {' '}
            <button className="btn btn-secondary" onClick={() => removeFromCart(item.productId)}>Remove</button>
          </div>
        </div>
      ))}
      <h3>Total: ${total.toFixed(2)}</h3>
      <button className="btn" onClick={handleCheckout}>Proceed to checkout</button>
    </div>
  );
}
