import { Link } from 'react-router-dom';

export default function Home() {
  return (
    <div className="hero">
      <h1>Handcrafted goods, made by real artisans.</h1>
      <p>Pottery, woodwork, textiles, and jewelry — one at a time, by hand.</p>
      <Link to="/products" className="btn">Browse Handcrafted Products</Link>
    </div>
  );
}
