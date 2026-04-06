import { AxiosResponse } from 'axios';
import { GenericResponsePagination, GUID } from '@/modules/core/types/types';

interface ListItemWithId {
  id: GUID;
}

/**
 * Helper para realizar un fetch de una lista de la API y actualizar el estado condicionalmente.
 * La llamada a la API siempre se realiza. El estado solo se actualiza si los datos han cambiado.
 *
 * @template T
 * @template F
 * @param {Object} options
 * @param {(filter: F) => Promise<AxiosResponse<GenericResponsePagination<T>>>} apiCall
 * @param {T[] | null} currentState
 * @param {React.Dispatch<React.SetStateAction<T[] | null>>} setState
 * @param {F} filter
 * @returns {Promise<GenericResponsePagination<T> | void>}
 */
export async function fetchAndSetList<T extends ListItemWithId, F>(options: {
  apiCall: (filter: F) => Promise<AxiosResponse<GenericResponsePagination<T>>>;
  currentState: T[] | null;
  setState: React.Dispatch<React.SetStateAction<T[] | null>>;
  filter: F;
}): Promise<GenericResponsePagination<T> | void> {
  const { apiCall, currentState, setState, filter } = options;

  const res: AxiosResponse<GenericResponsePagination<T>> =
    await apiCall(filter);

  if (res && res.data) {
    const newItems = res.data.items;

    const currentIds = (currentState || [])
      .map(item => item.id)
      .sort()
      .join(',');
    const newIds = newItems
      .map(item => item.id)
      .sort()
      .join(',');

    if (newIds !== currentIds) {
      setState(newItems);
    }

    return res.data;
  }

  return undefined;
}
