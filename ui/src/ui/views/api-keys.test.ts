import { render } from "lit";
import { describe, expect, it } from "vitest";
import { renderApiKeys } from "./api-keys.ts";

describe("renderApiKeys", () => {
  it("renders provider-owned API and account login methods", () => {
    const container = document.createElement("div");
    render(
      renderApiKeys({
        loading: false,
        error: null,
        result: {
          ts: Date.now(),
          providers: [
            {
              provider: "openai",
              displayName: "OpenAI",
              status: "ok",
              profiles: [{ profileId: "openai:default", type: "oauth", status: "ok" }],
            },
          ],
        },
        onRefresh: () => undefined,
      }),
      container,
    );

    expect(container.querySelectorAll(".provider-auth-card")).toHaveLength(3);
    expect(container.querySelector('[data-provider="openai"]')?.textContent).toContain("Connected");
    expect(container.textContent).toContain("Sign in with ChatGPT");
    expect(container.textContent).toContain("Use Claude CLI account");
    expect(container.textContent).toContain("Sign in with Google");
    const googleButtons = Array.from(
      container.querySelectorAll<HTMLButtonElement>('[data-provider="google"] .btn'),
    );
    expect(
      googleButtons.find((button) => button.title.includes("google-gemini-cli"))?.title,
    ).toContain("--method oauth");
  });
});
