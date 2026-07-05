import { FormEvent, useState } from 'react';
import {
  buildDatabaseConnectionString,
  cloneGitHubAppRepository,
  cloneGitRepository,
  setupOnPremAgent,
  submitOnboardingRepository,
  uploadOnboardingZip,
  validateRepositoryPath,
} from '../api/spaClient';
import type { GitHubAppStatus, OnboardingSession, OnPremAgentSetup } from '../types/spa';

type CodeMode = 'local' | 'zip' | 'github' | 'git';

interface OnboardingCodeStepProps {
  session: OnboardingSession;
  submitting: boolean;
  githubStatus: GitHubAppStatus | null;
  onSessionUpdated: (session: OnboardingSession) => void;
  onError: (message: string) => void;
  runStep: <T>(action: () => Promise<T>, onSuccess?: (result: T) => void) => Promise<void>;
}

export function OnboardingCodeStep({
  session,
  submitting,
  githubStatus,
  onSessionUpdated,
  onError,
  runStep,
}: OnboardingCodeStepProps) {
  const [mode, setMode] = useState<CodeMode>('local');
  const [repository, setRepository] = useState({
    repositoryName: '',
    repositoryPath: '',
    defaultBranch: 'main',
  });
  const [gitHub, setGitHub] = useState({
    repositoryName: '',
    owner: '',
    repository: '',
    defaultBranch: 'main',
    installationId: '',
  });
  const [git, setGit] = useState({
    repositoryName: '',
    repositoryUrl: '',
    defaultBranch: 'main',
    accessToken: '',
  });
  const [zipFile, setZipFile] = useState<File | null>(null);
  const [pathValidation, setPathValidation] = useState<string | null>(null);

  async function onSubmitLocal(event: FormEvent) {
    event.preventDefault();
    await runStep(
      () => submitOnboardingRepository(session.id, repository),
      (updated) => onSessionUpdated(updated),
    );
  }

  return (
    <div>
      <h2 className="h4 mb-3">Step 2 — Connect code</h2>
      <div className="btn-group mb-3" role="group" aria-label="Code connection mode">
        {(['local', 'zip', 'github', 'git'] as CodeMode[]).map((item) => (
          <button
            key={item}
            type="button"
            className={`btn btn-sm ${mode === item ? 'btn-primary' : 'btn-outline-primary'}`}
            onClick={() => setMode(item)}
          >
            {item === 'local' ? 'Local path' : item === 'zip' ? 'ZIP' : item === 'github' ? 'GitHub App' : 'Git URL'}
          </button>
        ))}
      </div>

      {mode === 'local' ? (
        <form onSubmit={(event) => void onSubmitLocal(event)}>
          <div className="row g-3">
            <div className="col-md-6">
              <label className="form-label">Repository name</label>
              <input
                className="form-control"
                required
                value={repository.repositoryName}
                onChange={(event) => setRepository({ ...repository, repositoryName: event.target.value })}
              />
            </div>
            <div className="col-md-6">
              <label className="form-label">Default branch</label>
              <input
                className="form-control"
                value={repository.defaultBranch}
                onChange={(event) => setRepository({ ...repository, defaultBranch: event.target.value })}
              />
            </div>
            <div className="col-12">
              <label className="form-label">Local repository path</label>
              <input
                className="form-control"
                required
                value={repository.repositoryPath}
                onChange={(event) => setRepository({ ...repository, repositoryPath: event.target.value })}
              />
            </div>
          </div>
          <div className="d-flex flex-wrap gap-2 mt-3">
            <button
              type="button"
              className="btn btn-outline-secondary"
              disabled={submitting || !repository.repositoryPath}
              onClick={() =>
                void runStep(() => validateRepositoryPath(repository.repositoryPath), (result) =>
                  setPathValidation(
                    result.isValid
                      ? `Valid — ${result.csharpFileCount} C# files, ${result.controllerCount} controllers`
                      : result.errorMessage ?? 'Path is not valid',
                  ),
                )
              }
            >
              Validate path
            </button>
            <button type="submit" className="btn btn-primary" disabled={submitting}>
              Continue to database
            </button>
          </div>
          {pathValidation ? <p className="small text-muted mt-2 mb-0">{pathValidation}</p> : null}
        </form>
      ) : null}

      {mode === 'zip' ? (
        <div>
          <label className="form-label">ZIP archive</label>
          <input
            type="file"
            className="form-control mb-3"
            accept=".zip"
            onChange={(event) => setZipFile(event.target.files?.[0] ?? null)}
          />
          <button
            type="button"
            className="btn btn-primary"
            disabled={submitting || !zipFile}
            onClick={() => {
              if (!zipFile) {
                onError('ZIP file is required.');
                return;
              }

              void runStep(
                () => uploadOnboardingZip(session.id, zipFile, repository.repositoryName || undefined),
                (updated) => onSessionUpdated(updated),
              );
            }}
          >
            Upload and continue
          </button>
        </div>
      ) : null}

      {mode === 'github' ? (
        <div>
          {githubStatus ? (
            <p className="small text-muted">
              GitHub App {githubStatus.isConfigured ? 'configured' : 'not configured'}
              {githubStatus.defaultInstallationId
                ? ` — installation ${githubStatus.defaultInstallationId}`
                : ''}
            </p>
          ) : null}
          <div className="row g-3">
            <div className="col-md-6">
              <label className="form-label">Repository name</label>
              <input
                className="form-control"
                value={gitHub.repositoryName}
                onChange={(event) => setGitHub({ ...gitHub, repositoryName: event.target.value })}
              />
            </div>
            <div className="col-md-3">
              <label className="form-label">Owner</label>
              <input
                className="form-control"
                required
                value={gitHub.owner}
                onChange={(event) => setGitHub({ ...gitHub, owner: event.target.value })}
              />
            </div>
            <div className="col-md-3">
              <label className="form-label">Repository</label>
              <input
                className="form-control"
                required
                value={gitHub.repository}
                onChange={(event) => setGitHub({ ...gitHub, repository: event.target.value })}
              />
            </div>
          </div>
          <button
            type="button"
            className="btn btn-primary mt-3"
            disabled={submitting}
            onClick={() =>
              void runStep(
                () =>
                  cloneGitHubAppRepository(session.id, {
                    repositoryName: gitHub.repositoryName || `${gitHub.owner}/${gitHub.repository}`,
                    owner: gitHub.owner,
                    repository: gitHub.repository,
                    defaultBranch: gitHub.defaultBranch,
                    installationId: gitHub.installationId
                      ? Number(gitHub.installationId)
                      : githubStatus?.defaultInstallationId,
                  }),
                (updated) => onSessionUpdated(updated),
              )
            }
          >
            Clone via GitHub App
          </button>
        </div>
      ) : null}

      {mode === 'git' ? (
        <div>
          <div className="row g-3">
            <div className="col-md-6">
              <label className="form-label">Repository name</label>
              <input
                className="form-control"
                value={git.repositoryName}
                onChange={(event) => setGit({ ...git, repositoryName: event.target.value })}
              />
            </div>
            <div className="col-md-6">
              <label className="form-label">Git URL</label>
              <input
                className="form-control"
                required
                value={git.repositoryUrl}
                onChange={(event) => setGit({ ...git, repositoryUrl: event.target.value })}
              />
            </div>
          </div>
          <button
            type="button"
            className="btn btn-primary mt-3"
            disabled={submitting}
            onClick={() =>
              void runStep(
                () =>
                  cloneGitRepository(session.id, {
                    repositoryName: git.repositoryName || 'imported-repo',
                    repositoryUrl: git.repositoryUrl,
                    defaultBranch: git.defaultBranch,
                    accessToken: git.accessToken || undefined,
                  }),
                (updated) => onSessionUpdated(updated),
              )
            }
          >
            Clone repository
          </button>
        </div>
      ) : null}
    </div>
  );
}

type DatabaseMode = 'direct' | 'builder' | 'onprem';

interface OnboardingDatabaseStepProps {
  session: OnboardingSession;
  submitting: boolean;
  database: {
    connectionName: string;
    provider: string;
    connectionString: string;
    isReadOnly: boolean;
  };
  setDatabase: React.Dispatch<
    React.SetStateAction<{
      connectionName: string;
      provider: string;
      connectionString: string;
      isReadOnly: boolean;
    }>
  >;
  onSessionUpdated: (session: OnboardingSession) => void;
  onSkip: () => void;
  onSubmitDirect: (event: FormEvent) => void;
  runStep: <T>(action: () => Promise<T>, onSuccess?: (result: T) => void) => Promise<void>;
}

export function OnboardingDatabaseStep({
  session,
  submitting,
  database,
  setDatabase,
  onSessionUpdated,
  onSkip,
  onSubmitDirect,
  runStep,
}: OnboardingDatabaseStepProps) {
  const [mode, setMode] = useState<DatabaseMode>('direct');
  const [builder, setBuilder] = useState({
    host: 'localhost',
    port: 5432,
    database: 'enhancementhub',
    username: '',
    password: '',
    integratedSecurity: false,
  });
  const [onPremSetup, setOnPremSetup] = useState<OnPremAgentSetup | null>(null);

  return (
    <div>
      <h2 className="h4 mb-3">Step 3 — Connect database</h2>
      <div className="btn-group mb-3" role="group" aria-label="Database connection mode">
        {(['direct', 'builder', 'onprem'] as DatabaseMode[]).map((item) => (
          <button
            key={item}
            type="button"
            className={`btn btn-sm ${mode === item ? 'btn-primary' : 'btn-outline-primary'}`}
            onClick={() => setMode(item)}
          >
            {item === 'direct' ? 'Connection string' : item === 'builder' ? 'Builder' : 'On-prem agent'}
          </button>
        ))}
      </div>

      {mode === 'direct' ? (
        <form onSubmit={onSubmitDirect}>
          <div className="row g-3">
            <div className="col-md-6">
              <label className="form-label">Connection name</label>
              <input
                className="form-control"
                required
                value={database.connectionName}
                onChange={(event) => setDatabase({ ...database, connectionName: event.target.value })}
              />
            </div>
            <div className="col-md-6">
              <label className="form-label">Provider</label>
              <select
                className="form-select"
                value={database.provider}
                onChange={(event) => setDatabase({ ...database, provider: event.target.value })}
              >
                <option value="Sqlite">SQLite</option>
                <option value="PostgreSql">PostgreSQL</option>
                <option value="SqlServer">SQL Server</option>
              </select>
            </div>
            <div className="col-12">
              <label className="form-label">Connection string</label>
              <input
                className="form-control"
                required
                value={database.connectionString}
                onChange={(event) => setDatabase({ ...database, connectionString: event.target.value })}
              />
            </div>
          </div>
          <div className="d-flex flex-wrap gap-2 mt-4">
            <button type="submit" className="btn btn-primary" disabled={submitting}>
              Save and continue
            </button>
            <button type="button" className="btn btn-outline-secondary" disabled={submitting} onClick={onSkip}>
              Skip database
            </button>
          </div>
        </form>
      ) : null}

      {mode === 'builder' ? (
        <div>
          <div className="row g-3">
            <div className="col-md-4">
              <label className="form-label">Host</label>
              <input
                className="form-control"
                value={builder.host}
                onChange={(event) => setBuilder({ ...builder, host: event.target.value })}
              />
            </div>
            <div className="col-md-2">
              <label className="form-label">Port</label>
              <input
                type="number"
                className="form-control"
                value={builder.port}
                onChange={(event) => setBuilder({ ...builder, port: Number(event.target.value) })}
              />
            </div>
            <div className="col-md-6">
              <label className="form-label">Database</label>
              <input
                className="form-control"
                value={builder.database}
                onChange={(event) => setBuilder({ ...builder, database: event.target.value })}
              />
            </div>
          </div>
          <button
            type="button"
            className="btn btn-outline-secondary mt-3"
            disabled={submitting}
            onClick={() =>
              void runStep(
                () =>
                  buildDatabaseConnectionString({
                    provider: database.provider,
                    host: builder.host,
                    port: builder.port,
                    database: builder.database,
                    username: builder.username || undefined,
                    password: builder.password || undefined,
                    integratedSecurity: builder.integratedSecurity,
                  }),
                (result) => setDatabase({ ...database, connectionString: result.connectionString }),
              )
            }
          >
            Build connection string
          </button>
          {database.connectionString ? (
            <p className="small text-muted mt-2 mb-0">
              <code>{database.connectionString}</code>
            </p>
          ) : null}
        </div>
      ) : null}

      {mode === 'onprem' ? (
        <div>
          <button
            type="button"
            className="btn btn-primary"
            disabled={submitting || !session.applicationId}
            onClick={() =>
              void runStep(
                () =>
                  setupOnPremAgent(session.id, {
                    applicationId: session.applicationId!,
                    connectionName: database.connectionName || 'On-prem database',
                    provider: database.provider,
                  }),
                (setup) => {
                  setOnPremSetup(setup);
                  void runStep(() => Promise.resolve(session), (updated) => onSessionUpdated(updated));
                },
              )
            }
          >
            Register on-prem agent
          </button>
          {onPremSetup ? (
            <pre className="small bg-light p-3 mt-3 mb-0">{onPremSetup.agentConfigSnippet}</pre>
          ) : null}
        </div>
      ) : null}
    </div>
  );
}
