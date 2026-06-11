import clsx from 'clsx';
import Heading from '@theme/Heading';
import styles from './styles.module.css';
import  { Redirect } from 'react-router-dom';

export default function Home() {
  return <Redirect to='/docs' />;
}
