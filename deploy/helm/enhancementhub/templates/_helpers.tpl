{{- define "enhancementhub.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" }}
{{- end }}

{{- define "enhancementhub.fullname" -}}
{{- printf "%s-%s" .Release.Name (include "enhancementhub.name" .) | trunc 63 | trimSuffix "-" }}
{{- end }}
