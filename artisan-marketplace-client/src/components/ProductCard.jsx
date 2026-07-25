import { Link } from 'react-router-dom';

export default function ProductCard({ product }) {
  return (
    <div className="card">
      <Link to={`/products/${product.id}`}>
        <img src={product.imageUrl} alt={product.name} onError={(e) => { e.target.style.visibility = 'hidden'; }} />
        <h3>{product.name}</h3>
      </Link>
      <p className="price">${product.price.toFixed(2)}</p>
      <Link to={`/products/${product.id}`} className="btn">View</Link>
    </div>
  );
}
