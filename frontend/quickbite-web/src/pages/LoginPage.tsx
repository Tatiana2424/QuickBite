import { FormEvent, useState } from "react";
import { login } from "../services/quickbiteService";

export function LoginPage() {
  const [result, setResult] = useState("Use demo credentials after registering through the Identity API.");

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const formData = new FormData(event.currentTarget);
    const email = String(formData.get("email") ?? "");
    const password = String(formData.get("password") ?? "");

    try {
      const response = await login(email, password);
      setResult(`Authenticated as ${response.fullName}. JWT token received.`);
    } catch {
      setResult("Login failed. Register a user through the Identity API first.");
    }
  }

  return (
    <section className="panel">
      <p className="eyebrow">Identity</p>
      <h2>Login shell</h2>
      <form className="stack" onSubmit={handleSubmit}>
        <input name="email" type="email" placeholder="you@example.com" defaultValue="demo@quickbite.local" />
        <input name="password" type="password" placeholder="Password" defaultValue="Pass123!" />
        <button type="submit">Sign in</button>
      </form>
      <p className="muted">{result}</p>
    </section>
  );
}
