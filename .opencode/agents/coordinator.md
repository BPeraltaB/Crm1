description: SDD - coordina el flujo SDD completo con planner, implementer y reviewer y transmite el contexto entre fases
mode: primary
permissions:
-action; edit
resource: "*"
effect: deny
-action: Shell
resource: "*"
effect: deny
-action: webfetch
resource: "*"
effect: deny
-action: websearch
resource: "*"
effect: deny
-action: subagent
resource: "*"
effect: deny
-action: subagent
resource: "planner"
effect: allow
-action: subagent
resource: "implementer"
effect: allow
-action: subagent
resource: "reviewer"
effect: allow

