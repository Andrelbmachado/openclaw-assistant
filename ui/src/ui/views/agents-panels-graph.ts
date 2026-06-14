// Control UI view projects canonical agent configuration as a visual node graph.
import { html } from "lit";
import type { AgentsFilesListResult, AgentsListResult, CronJob } from "../types.ts";
import { buildAgentContext, resolveAgentConfig, resolveModelLabel } from "./agents-utils.ts";
import type { AgentsPanel } from "./agents.types.ts";

export function renderAgentGraph(params: {
  agent: AgentsListResult["agents"][number];
  defaultId: string | null;
  configForm: Record<string, unknown> | null;
  agentFilesList: AgentsFilesListResult | null;
  channelCount: number | null;
  cronJobs: CronJob[];
  skillCount: number | null;
  running: boolean;
  onSelectPanel: (panel: AgentsPanel) => void;
}) {
  const config = resolveAgentConfig(params.configForm, params.agent.id);
  const context = buildAgentContext(
    params.agent,
    params.configForm,
    params.agentFilesList,
    params.defaultId,
  );
  const files =
    params.agentFilesList?.agentId === params.agent.id ? params.agentFilesList.files : [];
  const promptFiles = files.filter((file) => /^(agents|soul|identity|user)\.md$/i.test(file.name));
  const toolProfile = config.entry?.tools?.profile ?? config.globalTools?.profile ?? "default";
  const model = resolveModelLabel(
    config.entry?.model ?? config.defaults?.model ?? params.agent.model,
  );
  const agentCronJobs = params.cronJobs.filter((job) => job.agentId === params.agent.id);

  const node = (
    className: string,
    eyebrow: string,
    title: string,
    detail: string,
    panel: AgentsPanel,
  ) => html`
    <button
      type="button"
      class="agent-flow-node ${className}"
      @click=${() => params.onSelectPanel(panel)}
    >
      <span class="agent-flow-node__port agent-flow-node__port--input"></span>
      <span class="agent-flow-node__eyebrow">${eyebrow}</span>
      <strong>${title}</strong>
      <small>${detail}</small>
      <span class="agent-flow-node__port agent-flow-node__port--output"></span>
    </button>
  `;

  return html`
    <section class="agent-flow-shell">
      <header class="agent-flow-header">
        <div>
          <span class="agent-flow-header__eyebrow">Agent topology</span>
          <h2>${context.identityName}</h2>
          <p>Canonical OpenClaw configuration rendered as a connected workflow.</p>
        </div>
        <span class="agent-flow-header__state ${params.running ? "is-running" : ""}">
          <span></span>${params.running ? "Running" : "Ready"}
        </span>
      </header>

      <div class="agent-flow-canvas">
        <svg
          class="agent-flow-lines"
          viewBox="0 0 1000 590"
          preserveAspectRatio="none"
          aria-hidden="true"
        >
          <path d="M 205 295 C 285 295, 280 105, 375 105" />
          <path d="M 205 295 C 285 295, 280 255, 375 255" />
          <path d="M 205 295 C 285 295, 280 405, 375 405" />
          <path d="M 555 105 C 650 105, 645 180, 735 180" />
          <path d="M 555 255 C 650 255, 645 295, 735 295" />
          <path d="M 555 405 C 650 405, 645 410, 735 410" />
        </svg>
        ${node(
          "agent-flow-node--identity",
          "Agent",
          context.identityName,
          params.agent.id,
          "overview",
        )}
        ${node(
          "agent-flow-node--prompt",
          "Prompt",
          promptFiles.length ? `${promptFiles.length} instruction files` : "Workspace instructions",
          context.workspace,
          "files",
        )}
        ${node("agent-flow-node--model", "Intelligence", model, context.runtime, "overview")}
        ${node("agent-flow-node--tools", "Capabilities", "Tools", toolProfile, "tools")}
        ${node(
          "agent-flow-node--skills",
          "Knowledge",
          "Skills",
          params.skillCount == null ? context.skillsLabel : `${params.skillCount} available`,
          "skills",
        )}
        ${node(
          "agent-flow-node--channels",
          "Delivery",
          "Channels",
          params.channelCount == null ? "Load connections" : `${params.channelCount} connected`,
          "channels",
        )}
        ${node(
          "agent-flow-node--cron",
          "Automation",
          "Schedules",
          `${agentCronJobs.length} jobs`,
          "cron",
        )}
      </div>
      <p class="agent-flow-hint">Select a node to open its canonical editor.</p>
    </section>
  `;
}
