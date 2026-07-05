import * as signalR from '@microsoft/signalr';
import { useCallback, useEffect, useRef, useState } from 'react';

export interface CollaborationComment {
  id: string;
  content: string;
  userDisplayName: string;
  isInternal: boolean;
  createdAt: string;
}

interface UseRequestCollaborationOptions {
  onAnalysisUpdated?: () => void;
}

export function useRequestCollaboration(
  requestId: string,
  { onAnalysisUpdated }: UseRequestCollaborationOptions = {},
) {
  const [presence, setPresence] = useState('Only you');
  const [liveComments, setLiveComments] = useState<CollaborationComment[]>([]);
  const [analysisUpdateMessage, setAnalysisUpdateMessage] = useState<string | null>(null);
  const viewersRef = useRef(new Map<string, string>());
  const onAnalysisUpdatedRef = useRef(onAnalysisUpdated);

  onAnalysisUpdatedRef.current = onAnalysisUpdated;

  const updatePresence = useCallback(() => {
    const names = [...viewersRef.current.values()].filter(Boolean);
    setPresence(names.length === 0 ? 'Only you' : `Viewing: ${names.join(', ')}`);
  }, []);

  useEffect(() => {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/request-collaboration')
      .withAutomaticReconnect()
      .build();

    connection.on('CommentAdded', (payload: CollaborationComment) => {
      setLiveComments((prev) => {
        if (prev.some((comment) => comment.id === payload.id)) {
          return prev;
        }

        return [payload, ...prev];
      });
    });

    connection.on('UserJoined', (payload: { connectionId: string; userName: string }) => {
      viewersRef.current.set(payload.connectionId, payload.userName);
      updatePresence();
    });

    connection.on('UserLeft', (payload: { connectionId: string }) => {
      viewersRef.current.delete(payload.connectionId);
      updatePresence();
    });

    connection.on('AnalysisUpdated', (payload: { version: number }) => {
      setAnalysisUpdateMessage(`Analysis updated (v${payload.version}). Refreshing…`);
      onAnalysisUpdatedRef.current?.();
      window.setTimeout(() => setAnalysisUpdateMessage(null), 4000);
    });

    let cancelled = false;

    void connection
      .start()
      .then(() => {
        if (!cancelled) {
          return connection.invoke('JoinRequest', requestId);
        }

        return undefined;
      })
      .then(() => updatePresence())
      .catch(() => undefined);

    return () => {
      cancelled = true;
      void connection.invoke('LeaveRequest', requestId).finally(() => connection.stop());
    };
  }, [requestId, updatePresence]);

  return { presence, liveComments, analysisUpdateMessage };
}
