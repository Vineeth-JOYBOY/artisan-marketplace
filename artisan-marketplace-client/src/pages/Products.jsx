import { useEffect, useState } from 'react';
import client from '../api/client';
import ProductCard from '../components/ProductCard.jsx';

export default function Products() {
  const [categories, setCategories] = useState([]);
  const [products, setProducts] = useState([]);
  const [categoryId, setCategoryId] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    client.get('/categories').then((res) => setCategories(res.data)).catch(() => {});
  }, []);

  useEffect(() => {
    setLoading(true);
    setError('');
    const params = categoryId ? { categoryId } : {};
    client
      .get('/products', { params })
      .then((res) => setProducts(res.data))
      .catch(() => setError('Could not load products. Is the API Gateway running?'))
      .finally(() => setLoading(false));
  }, [categoryId]);

  return (
    <div>
      <h2>Handcrafted Products</h2>
      <div>
        <span
          className={`category-pill ${categoryId === null ? 'active' : ''}`}
          onClick={() => setCategoryId(null)}
        >
          All
        </span>
        {categories.map((c) => (
          <span
            key={c.id}
            className={`category-pill ${categoryId === c.id ? 'active' : ''}`}
            onClick={() => setCategoryId(c.id)}
          >
            {c.name}
          </span>
        ))}
      </div>

      {error && <p className="error">{error}</p>}
      {loading ? (
        <p>Loading...</p>
      ) : (
        <div className="grid">
          {products.map((p) => (
            <ProductCard key={p.id} product={p} />
          ))}
        </div>
      )}
    </div>
  );
}
