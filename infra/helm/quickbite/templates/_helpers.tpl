{{- define "quickbite.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{- define "quickbite.fullname" -}}
{{- if .Values.fullnameOverride -}}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" -}}
{{- else -}}
{{- printf "%s-%s" .Release.Name (include "quickbite.name" .) | trunc 63 | trimSuffix "-" -}}
{{- end -}}
{{- end -}}

{{- define "quickbite.labels" -}}
helm.sh/chart: {{ .Chart.Name }}-{{ .Chart.Version | replace "+" "_" }}
app.kubernetes.io/name: {{ include "quickbite.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
app.kubernetes.io/part-of: quickbite
{{- end -}}

{{- define "quickbite.componentName" -}}
{{- printf "%s-%s" (include "quickbite.fullname" .root) .component.name | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{- define "quickbite.componentLabels" -}}
{{ include "quickbite.labels" .root }}
app.kubernetes.io/component: {{ .component.name }}
quickbite.io/component-kind: {{ .component.kind }}
{{- end -}}

{{- define "quickbite.selectorLabels" -}}
app.kubernetes.io/name: {{ include "quickbite.name" .root }}
app.kubernetes.io/instance: {{ .root.Release.Name }}
app.kubernetes.io/component: {{ .component.name }}
{{- end -}}

{{- define "quickbite.secretName" -}}
{{- if .Values.secrets.create -}}
{{- printf "%s-runtime-secrets" (include "quickbite.fullname" .) | trunc 63 | trimSuffix "-" -}}
{{- else -}}
{{- .Values.secrets.existingSecret -}}
{{- end -}}
{{- end -}}
