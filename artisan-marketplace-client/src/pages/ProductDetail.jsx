import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import client from '../api/client';
import { useCart } from '../context/CartContext.jsx';

export default function ProductDetail() {
  const { id } = useParams();
  const [product, setProduct] = useState(null);
  const [quantity, setQuantity] = useState(1);
  const { addToCart } = useCart();
  const navigate = useNavigate();

  useEffect(() => {
    client.get(`/products/${id}`).then((res) => setProduct(res.data)).catch(() => setProduct(undefined));
  }, [id]);

  if (product === undefined) return <p className="error">Product not found.</p>;
  if (!product) return <p>Loading...</p>;

  function handleAddToCart() {
    addToCart(product, quantity);
    navigate('/cart');
  }

  return (
    <div>
      <img src={product.imageUrl} alt={product.name} style={{ maxWidth: 320, borderRadius: 8 }} onError={(e) => { e.target.style.visibility = 'hidden'; }} />
      <h2>{product.name}</h2>
      <p>{product.category?.name}</p>
      <p>{product.description}</p>
      <p className="price">${product.price.toFixed(2)}</p>
      <p>{product.stockQuantity} in stock</p>
      <input
        type="number"
        min="1"
        max={product.stockQuantity}
        value={quantity}
        onChange={(e) => setQuantity(Number(e.target.value))}
        style={{ width: 80 }}
      />
      <br />
      <button className="btn" onClick={handleAddToCart} disabled={product.stockQuantity === 0}>
        Add to cart
      </button>
    </div>
  );
}
