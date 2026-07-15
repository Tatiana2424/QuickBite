import { Link } from "react-router-dom";

export function SupportPage() {
  return (
    <section className="stack">
      <div className="page-heading">
        <p className="eyebrow">Support</p>
        <h1>How can we help?</h1>
        <p className="muted">Choose the closest situation. QuickBite keeps the next step simple and avoids asking for technical details.</p>
      </div>
      <section className="support-grid" aria-label="Support options">
        <article className="panel support-card">
          <p className="eyebrow">Payment</p>
          <h2>Payment failed or is still processing</h2>
          <p className="muted">Check your order details first. If payment failed, you can place the order again when ready or try another payment method once payments are connected.</p>
          <Link className="text-link" to="/orders">Review orders</Link>
        </article>
        <article className="panel support-card">
          <p className="eyebrow">Delivery</p>
          <h2>Delivery looks delayed</h2>
          <p className="muted">Order details refresh automatically while delivery is active. If courier status is unavailable, refresh again shortly.</p>
          <Link className="text-link" to="/orders">Track delivery</Link>
        </article>
        <article className="panel support-card">
          <p className="eyebrow">Contact</p>
          <h2>Still need help?</h2>
          <p className="muted">Send the order id and the email on your account. We will keep the message human-readable.</p>
          <a className="button-link button-link--primary" href="mailto:support@quickbite.local">Email support</a>
        </article>
      </section>
    </section>
  );
}
