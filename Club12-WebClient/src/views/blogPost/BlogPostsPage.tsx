import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { DataGrid, GridColDef, GridPaginationModel } from '@mui/x-data-grid';
import { formatDateAr } from '@/modules/core/utils/formatDate';
import {
  Box,
  Chip,
  InputAdornment,
  TextField,
} from '@mui/material';
import { useNavigate } from 'react-router-dom';
import { confirmDelete, notifySuccess } from '@/modules/core/utils/confirmDialog';
import { useBlogPost } from '@/modules/blogPost/hook/blogPost.hook';
import {
  BlogPostResponse,
  GetBlogPostsFilteredRequest,
} from '@/modules/blogPost/type/blogPost';
import { buildActionsColumn } from '@/views/core/components/buildActionsColumn';
import { TableRowAction } from '@/views/core/components/TableRowActions';
import NewEntityButton from '@/views/core/components/NewEntityButton';
import PageShell from '@/views/core/components/PageShell';
import FilterBar from '@/views/core/components/FilterBar';
import { TableSkeleton } from '@/views/core/components/skeletons';
import {
  DeleteIcon,
  EditIcon,
  SearchIcon,
  VisibilityIcon,
} from '@/views/core/MUI/icons/icons';
import {
  TABLE_PAGE_SIZE_OPTIONS,
  TABLE_ROWS_PER_PAGE,
} from '@/modules/core/constants/pagination';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';

const EMPTY_FILTERS: GetBlogPostsFilteredRequest = {};

const formatDate = (value: Date | string) => formatDateAr(value);

const BlogPostsPage: React.FC = () => {
  const { getBlogPostsByFilters, deleteBlogPostById } = useBlogPost();
  const navigate = useNavigate();
  const [posts, setPosts] = useState<BlogPostResponse[]>([]);
  const [loading, setLoading] = useState(false);
  const [rowCount, setRowCount] = useState(0);
  const [filters, setFilters] = useState<GetBlogPostsFilteredRequest>(EMPTY_FILTERS);
  const [paginationModel, setPaginationModel] = useState<GridPaginationModel>({
    page: 0,
    pageSize: TABLE_ROWS_PER_PAGE,
  });
  const getBlogPostsByFiltersRef = useRef(getBlogPostsByFilters);

  useEffect(() => {
    getBlogPostsByFiltersRef.current = getBlogPostsByFilters;
  }, [getBlogPostsByFilters]);

  const fetchPosts = useCallback(
    async (activeFilters: GetBlogPostsFilteredRequest, activePaginationModel: GridPaginationModel) => {
      setLoading(true);
      const response = await getBlogPostsByFiltersRef.current({
        ...activeFilters,
        pageNumber: activePaginationModel.page + 1,
        pageSize: activePaginationModel.pageSize,
      });
      if (response) {
        setPosts(response.items);
        setRowCount(response.totalCount);
      }
      setLoading(false);
    },
    []
  );

  useEffect(() => {
    void fetchPosts(filters, paginationModel);
  }, [fetchPosts, filters, paginationModel]);

  const handleFilterChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setFilters(prev => ({ ...prev, [name]: value || undefined }));
    setPaginationModel(prev => (prev.page === 0 ? prev : { ...prev, page: 0 }));
  };

  const handleClearFilters = () => {
    setFilters(EMPTY_FILTERS);
    setPaginationModel(prev => (prev.page === 0 ? prev : { ...prev, page: 0 }));
  };

  const handlePaginationModelChange = useCallback(
    (nextPaginationModel: GridPaginationModel) => {
      setPaginationModel(prev =>
        prev.page === nextPaginationModel.page &&
        prev.pageSize === nextPaginationModel.pageSize
          ? prev
          : nextPaginationModel
      );
    },
    []
  );

  const handleView = useCallback(
    (row: BlogPostResponse) => {
      navigate(APP_ROUTES.blogPost.build(row.slug), { state: { post: row } });
    },
    [navigate]
  );

  const handleEdit = useCallback(
    (row: BlogPostResponse) => {
      navigate(APP_ROUTES.panelBlogEdit.build(row.id));
    },
    [navigate]
  );

  const handleDelete = useCallback(
    async (row: BlogPostResponse) => {
      const confirmed = await confirmDelete({
        title: '¿Está usted seguro de querer eliminar esta publicación?',
        text: '¡Usted no podrá revertir este cambio!',
      });

      if (!confirmed) {
        return;
      }

      await deleteBlogPostById(row.id);
      await notifySuccess({ title: '¡Eliminada!', text: 'La publicación ha sido eliminada.' });
      await fetchPosts(filters, paginationModel);
    },
    [deleteBlogPostById, fetchPosts, filters, paginationModel]
  );

  const postActions = useMemo<TableRowAction<BlogPostResponse>[]>(
    () => [
      { label: 'Ver', color: 'info', icon: <VisibilityIcon fontSize="small" />, onClick: handleView },
      { label: 'Editar', color: 'primary', icon: <EditIcon fontSize="small" />, onClick: handleEdit },
      { label: 'Eliminar', color: 'error', icon: <DeleteIcon fontSize="small" />, onClick: handleDelete },
    ],
    [handleDelete, handleEdit, handleView]
  );

  const columns: GridColDef<BlogPostResponse>[] = useMemo(() => {
    const baseColumns: GridColDef<BlogPostResponse>[] = [
      { field: 'title', headerName: 'Título', flex: 1.4, minWidth: 200 },
      { field: 'author', headerName: 'Autor', flex: 1, minWidth: 150 },
      {
        field: 'isPublished',
        headerName: 'Estado',
        flex: 0.7,
        minWidth: 120,
        sortable: false,
        filterable: false,
        renderCell: params =>
          params.row.isPublished ? (
            <Chip size="small" color="success" variant="outlined" label="Publicada" />
          ) : (
            <Chip size="small" color="warning" label="Borrador" />
          ),
      },
      { field: 'views', headerName: 'Vistas', flex: 0.5, minWidth: 90 },
      {
        field: 'createdAt',
        headerName: 'Fecha',
        flex: 0.8,
        minWidth: 120,
        renderCell: params => formatDate(params.row.createdAt),
      },
    ];

    return [...baseColumns, buildActionsColumn(postActions)];
  }, [postActions]);

  const rows = useMemo(() => posts, [posts]);

  const handleCreate = useCallback(() => {
    navigate(APP_ROUTES.panelBlogCreate);
  }, [navigate]);

  return (
    <PageShell
      title="Blog"
      actions={<NewEntityButton type="Publicación" onClick={handleCreate} />}
    >
      <FilterBar onClear={handleClearFilters} ariaLabel="Filtros de publicaciones">
        <TextField
          label="Título"
          name="title"
            size="small"
            value={filters.title ?? ''}
            onChange={handleFilterChange}
            slotProps={{
              input: {
                startAdornment: (
                  <InputAdornment position="start">
                    <SearchIcon fontSize="small" />
                  </InputAdornment>
                ),
              }
            }}
          />
          <TextField
            label="Autor"
            name="author"
            size="small"
            value={filters.author ?? ''}
            onChange={handleFilterChange}
            slotProps={{
              input: {
                startAdornment: (
                  <InputAdornment position="start">
                    <SearchIcon fontSize="small" />
                  </InputAdornment>
                ),
              }
            }}
          />
      </FilterBar>

      {loading ? (
        <TableSkeleton columns={6} />
      ) : (
        <Box sx={{ width: '100%' }}>
          <DataGrid
            rows={rows}
            columns={columns}
            getRowId={row => row.id}
            autoHeight
            disableRowSelectionOnClick
            disableColumnMenu
            pageSizeOptions={TABLE_PAGE_SIZE_OPTIONS}
            paginationModel={paginationModel}
            onPaginationModelChange={handlePaginationModelChange}
            paginationMode="server"
            rowCount={rowCount}
          />
        </Box>
      )}
    </PageShell>
  );
};

export default BlogPostsPage;
