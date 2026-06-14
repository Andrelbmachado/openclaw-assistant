// Control UI view renders provider credentials without handling secret values in the browser.
import { html, nothing } from "lit";
import type { ModelAuthStatusProvider, ModelAuthStatusResult } from "../types.ts";

type AuthMethod = {
  id: string;
  label: string;
  description: string;
  provider: string;
  method: string;
};

type ProviderCard = {
  id: string;
  name: string;
  eyebrow: string;
  description: string;
  statusProviders: string[];
  methods: AuthMethod[];
};

const PROVIDERS: ProviderCard[] = [
  {
    id: "openai",
    name: "OpenAI",
    eyebrow: "GPT + Codex",
    description: "Use direct API billing or your supported ChatGPT/Codex subscription.",
    statusProviders: ["openai"],
    methods: [
      {
        id: "openai-api-key",
        label: "Connect API key",
        description: "The terminal asks for the key so it never passes through the web page.",
        provider: "openai",
        method: "api-key",
      },
      {
        id: "openai-account",
        label: "Sign in with ChatGPT",
        description: "Browser OAuth for an eligible ChatGPT or Codex subscription.",
        provider: "openai",
        method: "oauth",
      },
    ],
  },
  {
    id: "anthropic",
    name: "Anthropic",
    eyebrow: "Claude",
    description: "Connect an Anthropic API key or reuse a Claude CLI login on this computer.",
    statusProviders: ["anthropic", "claude-cli"],
    methods: [
      {
        id: "anthropic-api-key",
        label: "Connect API key",
        description: "Adds an Anthropic API key through the provider-owned secure flow.",
        provider: "anthropic",
        method: "api-key",
      },
      {
        id: "anthropic-account",
        label: "Use Claude CLI account",
        description: "Reuses a Claude CLI session already authenticated on this host.",
        provider: "anthropic",
        method: "cli",
      },
    ],
  },
  {
    id: "google",
    name: "Google Gemini",
    eyebrow: "Gemini",
    description: "Use an AI Studio key or connect a Google account through Gemini CLI OAuth.",
    statusProviders: ["google", "google-gemini-cli"],
    methods: [
      {
        id: "google-api-key",
        label: "Connect API key",
        description: "Adds a Gemini API key from Google AI Studio.",
        provider: "google",
        method: "api-key",
      },
      {
        id: "google-account",
        label: "Sign in with Google",
        description: "Starts Gemini CLI OAuth with a localhost browser callback.",
        provider: "google-gemini-cli",
        method: "oauth",
      },
    ],
  },
];

type DesktopWebView = {
  postMessage: (message: unknown) => void;
};

function desktopWebView(): DesktopWebView | null {
  const host = window as unknown as { chrome?: { webview?: DesktopWebView } };
  return host.chrome?.webview ?? null;
}

function authCommand(method: AuthMethod): string {
  return `openclaw models auth login --provider ${method.provider} --method ${method.method} --set-default`;
}

async function startAuth(method: AuthMethod): Promise<void> {
  const bridge = desktopWebView();
  if (bridge) {
    bridge.postMessage({ type: "openclaw.auth", provider: method.provider, method: method.method });
    return;
  }
  await navigator.clipboard.writeText(authCommand(method));
}

function resolveStatuses(
  result: ModelAuthStatusResult | null,
  providerIds: string[],
): ModelAuthStatusProvider[] {
  return (result?.providers ?? []).filter((entry) => providerIds.includes(entry.provider));
}

function statusLabel(statuses: ModelAuthStatusProvider[]): string {
  if (statuses.some((entry) => entry.status === "ok" || entry.status === "static")) {
    return "Connected";
  }
  if (statuses.some((entry) => entry.status === "expiring")) {
    return "Expiring soon";
  }
  if (statuses.some((entry) => entry.status === "expired")) {
    return "Needs attention";
  }
  return "Not connected";
}

export function renderApiKeys(props: {
  loading: boolean;
  error: string | null;
  result: ModelAuthStatusResult | null;
  onRefresh: () => void;
}) {
  const isDesktop = Boolean(desktopWebView());
  return html`
    <section class="provider-auth">
      <header class="provider-auth__hero">
        <div>
          <div class="provider-auth__eyebrow">Model connections</div>
          <h2>API keys and accounts</h2>
          <p>
            Each provider keeps its own authentication contract. Secrets are entered in the provider
            CLI, never inside this web page.
          </p>
        </div>
        <button class="btn btn--sm" ?disabled=${props.loading} @click=${props.onRefresh}>
          ${props.loading ? "Refreshing..." : "Refresh status"}
        </button>
      </header>

      ${props.error ? html`<div class="callout danger">${props.error}</div>` : nothing}

      <div class="provider-auth__grid">
        ${PROVIDERS.map((provider) => {
          const statuses = resolveStatuses(props.result, provider.statusProviders);
          const label = statusLabel(statuses);
          const connected = label === "Connected";
          return html`
            <article class="provider-auth-card" data-provider=${provider.id}>
              <div class="provider-auth-card__topline">
                <span class="provider-auth-card__mark">${provider.name.slice(0, 1)}</span>
                <span class="provider-auth-card__status ${connected ? "is-connected" : ""}">
                  <span></span>${label}
                </span>
              </div>
              <div class="provider-auth-card__eyebrow">${provider.eyebrow}</div>
              <h3>${provider.name}</h3>
              <p>${provider.description}</p>
              <div class="provider-auth-card__methods">
                ${provider.methods.map(
                  (method) => html`
                    <div class="provider-auth-method">
                      <div>
                        <strong>${method.label}</strong>
                        <span>${method.description}</span>
                      </div>
                      <button
                        type="button"
                        class="btn btn--sm ${method.method === "api-key" ? "primary" : ""}"
                        title=${isDesktop
                          ? "Open the secure provider login terminal"
                          : `Copy: ${authCommand(method)}`}
                        @click=${() => void startAuth(method)}
                      >
                        ${isDesktop ? "Open" : "Copy command"}
                      </button>
                    </div>
                  `,
                )}
              </div>
            </article>
          `;
        })}
      </div>

      <div class="provider-auth__note">
        <strong>${isDesktop ? "Desktop mode" : "Web mode"}</strong>
        <span
          >${isDesktop
            ? "OpenClaw Desktop launches the official interactive authentication command."
            : "The command is copied so you can run it in a terminal on the Gateway host."}</span
        >
      </div>
    </section>
  `;
}
